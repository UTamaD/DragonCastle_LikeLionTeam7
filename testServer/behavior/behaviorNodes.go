package behavior

import (
	"fmt"
	"log"
	"math"
	"math/rand"
	"sync"
	pb "testServer/Messages"
	"testServer/common"
	"time"
)

// Attack 노드 공유 상태
type AttackState struct {
	mutex          sync.Mutex
	currentAttack  int32
	lastAttackTime time.Time
	isWaiting      bool      // Wait 상태 추가
	waitUntil      time.Time // Wait 종료 시간 추가
}

const (
	NoAttack         = iota
	MeleeAttackType1 // 기존 근접 공격
	MeleeAttackType2 // 새로운 근접 공격 1
	MeleeAttackType3 // 새로운 근접 공격 2
	RangedAttackType
	MeteorAttackType
)

var globalAttackState = &AttackState{}

type Status int

const (
	Success Status = iota
	Failure
	Running
)

type Node interface {
	Execute() Status
}

type Sequence struct {
	children   []Node
	lastCheck  time.Time
	checkDelay time.Duration
}

func NewSequence(children ...Node) *Sequence {
	return &Sequence{children: children}
}

func (s *Sequence) Execute() Status {
	now := time.Now()
	if now.Sub(s.lastCheck) < s.checkDelay {
		return Running
	}
	s.lastCheck = now

	for _, child := range s.children {
		switch child.Execute() {
		case Failure:
			return Failure
		case Running:
			return Running
		}
	}
	return Success
}

type Selector struct {
	children   []Node
	lastCheck  time.Time
	checkDelay time.Duration
}

func NewSelector(children ...Node) *Selector {
	return &Selector{children: children}
}

func (s *Selector) Execute() Status {
	now := time.Now()
	if now.Sub(s.lastCheck) < s.checkDelay {
		return Running
	}
	s.lastCheck = now
	for i, child := range s.children {
		switch child.Execute() {
		case Success:
			log.Printf("Selector: Child %d succeeded, selector succeeding", i)
			return Success
		case Running:
			return Running
		}
	}
	return Failure
}

type Wait struct {
	duration  time.Duration
	startTime time.Time
	started   bool
	state     *AttackState
}

func NewWait(duration time.Duration, resetOnFailure bool, state *AttackState) *Wait {
	return &Wait{
		duration: duration,
		started:  false,
		state:    state,
	}
}

func (w *Wait) Execute() Status {
	w.state.mutex.Lock()
	defer w.state.mutex.Unlock()

	if !w.started {
		w.startTime = time.Now()
		w.started = true
		w.state.isWaiting = true
		w.state.waitUntil = w.startTime.Add(w.duration)
		return Running
	}

	if time.Now().After(w.state.waitUntil) {
		w.started = false
		w.state.isWaiting = false
		w.state.currentAttack = NoAttack
		return Success
	}
	return Running
}

type DetectPlayer struct {
	monster    common.IMonster
	range_     float32
	p          common.IPlayerManager
	n          common.INetworkManager
	lastCheck  time.Time
	checkDelay time.Duration
}

func NewDetectPlayer(monster common.IMonster, range_ float32, p common.IPlayerManager, n common.INetworkManager) *DetectPlayer {
	return &DetectPlayer{
		monster:    monster,
		range_:     range_,
		p:          p,
		n:          n,
		checkDelay: 500 * time.Millisecond,
	}
}

func (d *DetectPlayer) FindTarget() *common.Point {
	points := d.p.ListPoints()
	if len(points) > 0 {
		return &common.Point{
			X:        points[0].X,
			Z:        points[0].Z,
			PlayerId: points[0].PlayerId,
		}
	}
	return nil
}

func (d *DetectPlayer) Execute() Status {
	now := time.Now()
	if now.Sub(d.lastCheck) < d.checkDelay {
		return Running
	}
	d.lastCheck = now

	target := d.FindTarget()
	d.monster.SetTarget(target)
	if target == nil {
		return Failure
	}

	pos := d.monster.GetPosition()
	dist := distance(pos.X, pos.Z, target.X, target.Z)

	if dist <= d.range_ {
		return Success
	}
	return Failure
}

type AttackType int

const (
	MeleeAttack AttackType = iota
	RangedAttack
	MeteorAttack
)

type Attack struct {
	monster     common.IMonster
	range_      float32
	damage      int
	cooldown    time.Duration
	p           common.IPlayerManager
	n           common.INetworkManager
	attackType  int32
	state       *AttackState
	actionState *ActionState
}

func NewAttack(monster common.IMonster, range_ float32, damage int, cooldown time.Duration,
	p common.IPlayerManager, n common.INetworkManager, attackType int32, state *AttackState) *Attack {
	return &Attack{
		monster:     monster,
		range_:      range_,
		damage:      damage,
		cooldown:    cooldown,
		p:           p,
		n:           n,
		attackType:  attackType,
		state:       state,
		actionState: NewActionState(),
	}
}

func (a *Attack) Execute() Status {
	if !a.actionState.SetAction(fmt.Sprintf("attack_%d", a.attackType)) {
		return Failure
	}
	defer a.actionState.ClearAction()

	a.state.mutex.Lock()
	defer a.state.mutex.Unlock()

	// Wait 중이면 실행하지 않음
	if a.state.isWaiting {
		return Failure
	}

	// 다른 공격이 진행 중이면서 현재 공격과 다른 경우 실행하지 않음
	if a.state.currentAttack != NoAttack && a.state.currentAttack != a.attackType {
		return Failure
	}

	// 쿨다운 체크
	if time.Since(a.state.lastAttackTime) < a.cooldown {
		return Running
	}

	if a.monster.IsDead() {
		return Failure
	}

	target := a.monster.GetTarget()
	if target == nil {
		return Failure
	}

	pos := a.monster.GetPosition()
	dist := distance(pos.X, pos.Z, target.X, target.Z)
	if dist > a.range_ {
		return Failure
	}

	// 공격 상태 설정
	a.state.currentAttack = a.attackType

	// 원거리 공격인 경우 투사체 메시지 전송
	if a.attackType == int32(RangedAttack) {
		projectileMsg := &pb.GameMessage{
			Message: &pb.GameMessage_MonsterProjectile{
				MonsterProjectile: &pb.MonsterProjectile{
					MonsterId:    int32(a.monster.GetID()),
					StartX:       pos.X,
					StartZ:       pos.Z,
					TargetX:      target.X,
					TargetZ:      target.Z,
					ProjectileId: 1,
				},
			},
		}
		a.p.Broadcast(projectileMsg)
	}

	// 공격 메시지 전송
	attackMsg := &pb.GameMessage{
		Message: &pb.GameMessage_MonsterAttack{
			MonsterAttack: &pb.MonsterAttack{
				MonsterId:      int32(a.monster.GetID()),
				TargetPlayerId: target.PlayerId,
				AttackType:     a.attackType,
				Damage:         float32(a.damage),
			},
		},
	}
	a.p.Broadcast(attackMsg)

	// 마지막 공격 시간 업데이트
	a.state.lastAttackTime = time.Now()

	return Success
}

type MeleeAttackNode struct {
	*Attack
}

func NewMeleeAttack(monster common.IMonster, range_ float32, damage int,
	cooldown time.Duration, p common.IPlayerManager, n common.INetworkManager, state *AttackState, attackType int32) *MeleeAttackNode {
	return &MeleeAttackNode{
		Attack: NewAttack(monster, range_, damage, cooldown, p, n, attackType, state),
	}
}

type RangedAttackNode struct {
	*Attack
}

func NewRangedAttack(monster common.IMonster, range_ float32, damage int,
	cooldown time.Duration, p common.IPlayerManager, n common.INetworkManager, state *AttackState) *RangedAttackNode {
	return &RangedAttackNode{
		Attack: NewAttack(monster, range_, damage, cooldown, p, n, int32(RangedAttack), state),
	}
}

type Chase struct {
	monster      common.IMonster
	speed        float32
	p            common.IPlayerManager
	n            common.INetworkManager
	startTime    time.Time
	minDuration  time.Duration
	actionState  *ActionState
	acceleration float32
	maxSpeed     float32
	lastUpdate   time.Time
	currentSpeed float32 // 현재 속도
}

func NewChase(monster common.IMonster, speed float32, p common.IPlayerManager, n common.INetworkManager, actionState *ActionState) *Chase {
	return &Chase{
		monster:      monster,
		speed:        2.0,
		p:            p,
		n:            n,
		minDuration:  2 * time.Second,
		actionState:  actionState,
		acceleration: 1.1, //
		maxSpeed:     7.0, //
		lastUpdate:   time.Now(),
		currentSpeed: 2.0, //
	}
}
func (c *Chase) Execute() Status {
	if !c.actionState.SetAction("chase") {
		return Failure
	}
	defer c.actionState.ClearAction()

	target := c.monster.GetTarget()
	if target == nil {
		return Failure
	}

	if c.startTime.IsZero() {
		c.startTime = time.Now()
		c.lastUpdate = c.startTime
	}

	// 현재 시간과 경과 시간 계산
	now := time.Now()
	elapsedTime := now.Sub(c.startTime)

	if elapsedTime < c.minDuration {
		// 현재 속도 계산 (가속도 적용)
		currentSpeed := c.speed + float32(elapsedTime.Seconds())*c.acceleration
		if currentSpeed > c.maxSpeed {
			currentSpeed = c.maxSpeed
		}

		c.updatePositionWithSpeed(target, currentSpeed, now)
		return Running
	}

	c.startTime = time.Time{}
	return Success
}

func (c *Chase) updatePositionWithSpeed(target *common.Point, currentSpeed float32, now time.Time) {
	deltaTime := now.Sub(c.lastUpdate).Seconds()
	c.lastUpdate = now

	// 매우 약한 가속
	c.currentSpeed += float32(deltaTime) * c.acceleration * 0.2 // 가속도를 더욱 감소
	if c.currentSpeed > c.maxSpeed {
		c.currentSpeed = c.maxSpeed
	}

	pos := c.monster.GetPosition()
	dx := target.X - pos.X
	dy := target.Z - pos.Z
	dist := float32(math.Sqrt(float64(dx*dx + dy*dy)))

	if dist < 0.1 {
		return
	}

	moveX := (dx / dist) * c.currentSpeed * float32(deltaTime)
	moveZ := (dy / dist) * c.currentSpeed * float32(deltaTime)

	newX := pos.X + moveX
	newZ := pos.Z + moveZ
	c.monster.SetPosition(newX, newZ)

	MonsterMove := &pb.GameMessage{
		Message: &pb.GameMessage_MoveMonster{
			MoveMonster: &pb.MoveMonster{
				X:         newX,
				Z:         newZ,
				MonsterId: int32(c.monster.GetID()),
			},
		},
	}
	c.p.Broadcast(MonsterMove)
}

type MeteorAttackNode struct {
	monster     common.IMonster
	range_      float32
	damage      int
	cooldown    time.Duration
	p           common.IPlayerManager
	n           common.INetworkManager
	state       *AttackState
	actionState *ActionState
}

func NewMeteorAttack(monster common.IMonster, range_ float32, damage int,
	cooldown time.Duration, p common.IPlayerManager, n common.INetworkManager, state *AttackState) *MeteorAttackNode {
	return &MeteorAttackNode{
		monster:     monster,
		range_:      range_,
		damage:      damage,
		cooldown:    cooldown,
		p:           p,
		n:           n,
		state:       state,
		actionState: NewActionState(),
	}
}

func (m *MeteorAttackNode) Execute() Status {
	if !m.actionState.SetAction("meteor_attack") {
		return Failure
	}
	defer m.actionState.ClearAction()

	m.state.mutex.Lock()
	defer m.state.mutex.Unlock()

	// Wait 중이면 실행하지 않음
	if m.state.isWaiting {
		return Failure
	}

	if m.monster.IsDead() {
		return Failure
	}

	target := m.monster.GetTarget()
	if target == nil {
		return Failure
	}

	// 다른 공격이 진행 중이면서 현재 공격과 다른 경우 실행하지 않음
	if m.state.currentAttack != NoAttack && m.state.currentAttack != int32(MeteorAttack) {
		return Failure
	}

	// 쿨다운 체크
	if time.Since(m.state.lastAttackTime) < m.cooldown {
		return Running
	}

	monsterPos := m.monster.GetPosition()
	meteorPositions := make([]*pb.MeteorStrikePosition, 5)
	for i := 0; i < 5; i++ {
		angle := rand.Float64() * 2 * math.Pi
		distance := rand.Float32() * 40

		x := monsterPos.X + distance*float32(math.Cos(angle))
		z := monsterPos.Z + distance*float32(math.Sin(angle))

		meteorPositions[i] = &pb.MeteorStrikePosition{
			X: x,
			Z: z,
		}
	}

	meteorMsg := &pb.GameMessage{
		Message: &pb.GameMessage_MeteorStrike{
			MeteorStrike: &pb.MeteorStrike{
				MonsterId: int32(m.monster.GetID()),
				Positions: meteorPositions,
			},
		},
	}
	m.p.Broadcast(meteorMsg)

	attackMsg := &pb.GameMessage{
		Message: &pb.GameMessage_MonsterAttack{
			MonsterAttack: &pb.MonsterAttack{
				MonsterId:      int32(m.monster.GetID()),
				TargetPlayerId: target.PlayerId,
				AttackType:     int32(MeteorAttack),
				Damage:         float32(m.damage),
			},
		},
	}
	m.p.Broadcast(attackMsg)

	m.state.currentAttack = int32(MeteorAttack)
	m.state.lastAttackTime = time.Now()

	return Success
}

func distance(x1, y1, x2, y2 float32) float32 {
	dx := x2 - x1
	dy := y2 - y1
	return float32(math.Sqrt(float64(dx*dx + dy*dy)))
}

type MutuallyExclusiveSelector struct {
	children          []Node
	probability       float32
	lastChoice        int
	lastExecutionTime time.Time
	cooldown          time.Duration
}

func NewMutuallyExclusiveSelector(probability float32, children []Node) *MutuallyExclusiveSelector {
	return &MutuallyExclusiveSelector{
		children:    children,
		probability: probability,
		lastChoice:  -1,
		cooldown:    5 * time.Second,
	}
}

func (m *MutuallyExclusiveSelector) Execute() Status {
	if time.Since(m.lastExecutionTime) < m.cooldown {
		return Running
	}

	choice := rand.Intn(len(m.children))
	if choice == m.lastChoice && len(m.children) > 1 {
		choice = (choice + 1) % len(m.children)
	}

	status := m.children[choice].Execute()
	if status != Running {
		m.lastChoice = choice
		m.lastExecutionTime = time.Now()
	}

	return status
}

type RotateToTarget struct {
	monster     common.IMonster
	p           common.IPlayerManager
	n           common.INetworkManager
	rotateSpeed float32
	minAngle    float32
	viewAngle   float32
	actionState *ActionState
}

func NewRotateToTarget(monster common.IMonster, rotateSpeed float32, p common.IPlayerManager, n common.INetworkManager) *RotateToTarget {
	return &RotateToTarget{
		monster:     monster,
		rotateSpeed: rotateSpeed,
		p:           p,
		n:           n,
		minAngle:    10.0,
		viewAngle:   90.0,
		actionState: NewActionState(),
	}
}
func (r *RotateToTarget) Execute() Status {
	if !r.actionState.SetAction("rotate") {
		return Failure
	}
	defer r.actionState.ClearAction()

	target := r.monster.GetTarget()
	if target == nil {
		return Failure
	}

	pos := r.monster.GetPosition()
	currentRotation := r.monster.GetRotation()

	targetAngle := float32(math.Atan2(float64(target.Z-pos.Z), float64(target.X-pos.X)))
	angleDiffDegrees := math.Abs(float64(normalizeAngle(targetAngle-currentRotation))) * 180 / math.Pi

	if angleDiffDegrees <= float64(r.viewAngle/2) || angleDiffDegrees < float64(r.minAngle) {
		return Success
	}

	baseDuration := 3.0
	additionalTime := math.Floor(angleDiffDegrees / 90.0)
	totalDuration := baseDuration + additionalTime

	rotateMsg := &pb.GameMessage{
		Message: &pb.GameMessage_MonsterRotate{
			MonsterRotate: &pb.MonsterRotate{
				MonsterId: int32(r.monster.GetID()),
				Rotation:  targetAngle,
				Duration:  float32(totalDuration),
			},
		},
	}
	r.p.Broadcast(rotateMsg)

	time.Sleep(time.Duration(totalDuration * float64(time.Second)))

	r.monster.SetRotation(targetAngle)
	return Success
}

func normalizeAngle(angle float32) float32 {
	const twoPi = 2 * math.Pi
	normalized := float64(angle)
	normalized = math.Mod(normalized, twoPi)
	if normalized < -math.Pi {
		normalized += twoPi
	} else if normalized > math.Pi {
		normalized -= twoPi
	}
	return float32(normalized)
}

type PatternState struct {
	LastPattern  string
	PatternCount int
	MaxRepeat    int
}

type PatternTracker struct {
	state          *PatternState
	combatSelector Node
	chaseSequence  Node
}

func NewPatternTracker(state *PatternState, combatSelector, chaseSequence Node) *PatternTracker {
	if state == nil {
		state = &PatternState{
			MaxRepeat: 2,
		}
	}
	return &PatternTracker{
		state:          state,
		combatSelector: combatSelector,
		chaseSequence:  chaseSequence,
	}
}

func (p *PatternTracker) Execute() Status {
	var result Status

	if p.state.PatternCount >= p.state.MaxRepeat {
		if p.state.LastPattern == "chase" {
			result = p.combatSelector.Execute()
			if result == Success || result == Running {
				p.state.LastPattern = "combat"
				p.state.PatternCount = 1
			}
		} else {
			result = p.chaseSequence.Execute()
			if result == Success || result == Running {
				p.state.LastPattern = "chase"
				p.state.PatternCount = 1
			}
		}
		return result
	}

	if rand.Float32() < 0.25 {
		result = p.chaseSequence.Execute()
		if result == Success || result == Running {
			if p.state.LastPattern == "chase" {
				p.state.PatternCount++
			} else {
				p.state.LastPattern = "chase"
				p.state.PatternCount = 1
			}
		}
	} else {
		result = p.combatSelector.Execute()
		if result == Success || result == Running {
			if p.state.LastPattern == "combat" {
				p.state.PatternCount++
			} else {
				p.state.LastPattern = "combat"
				p.state.PatternCount = 1
			}
		}
	}

	return result
}

type RotateWithoutAnimation struct {
	monster common.IMonster
	p       common.IPlayerManager
	n       common.INetworkManager
}

func NewRotateWithoutAnimation(monster common.IMonster, p common.IPlayerManager, n common.INetworkManager) *RotateWithoutAnimation {
	return &RotateWithoutAnimation{
		monster: monster,
		p:       p,
		n:       n,
	}
}

func (r *RotateWithoutAnimation) Execute() Status {
	target := r.monster.GetTarget()
	if target == nil {
		return Failure
	}

	pos := r.monster.GetPosition()
	targetAngle := float32(math.Atan2(float64(target.Z-pos.Z), float64(target.X-pos.X)))
	r.monster.SetRotation(targetAngle)

	// Send rotation message to clients
	// Duration 0 indicates instant rotation without animation
	rotateMsg := &pb.GameMessage{
		Message: &pb.GameMessage_MonsterRotate{
			MonsterRotate: &pb.MonsterRotate{
				MonsterId: int32(r.monster.GetID()),
				Rotation:  targetAngle,
				Duration:  2,
			},
		},
	}
	r.p.Broadcast(rotateMsg)

	return Success
}

type InSightCheck struct {
	monster    common.IMonster
	viewAngle  float32
	lastCheck  time.Time
	checkDelay time.Duration
}

func NewInSightCheck(monster common.IMonster, viewAngle float32) *InSightCheck {
	return &InSightCheck{
		monster:   monster,
		viewAngle: viewAngle,
	}
}

func (i *InSightCheck) Execute() Status {
	now := time.Now()
	if now.Sub(i.lastCheck) < i.checkDelay {
		return Success
	}
	i.lastCheck = now

	target := i.monster.GetTarget()
	if target == nil {
		return Failure
	}

	pos := i.monster.GetPosition()
	currentRotation := i.monster.GetRotation()
	targetAngle := float32(math.Atan2(float64(target.Z-pos.Z), float64(target.X-pos.X)))
	angleDiff := math.Abs(float64(normalizeAngle(targetAngle-currentRotation))) * 180 / math.Pi

	if angleDiff <= float64(i.viewAngle/2) {
		return Success
	}

	return Failure
}

// behaviorNodes.go에 추가
type ActionState struct {
	mutex         sync.Mutex
	currentAction string // "none", "chase", "attack", "rotate" 등
}

func NewActionState() *ActionState {
	return &ActionState{
		currentAction: "none",
	}
}

func (a *ActionState) SetAction(action string) bool {
	a.mutex.Lock()
	defer a.mutex.Unlock()

	if a.currentAction != "none" {
		return false
	}

	a.currentAction = action
	return true
}

func (a *ActionState) ClearAction() {
	a.mutex.Lock()
	defer a.mutex.Unlock()
	a.currentAction = "none"
}

type RotationCheckBeforeChase struct {
	monster   common.IMonster
	viewAngle float32
	threshold float32
}

func NewRotationCheckBeforeChase(monster common.IMonster) *RotationCheckBeforeChase {
	return &RotationCheckBeforeChase{
		monster:   monster,
		viewAngle: 25.0, // 임계값
		threshold: 0.1,  // 정밀도 임계값
	}
}

func (r *RotationCheckBeforeChase) Execute() Status {
	target := r.monster.GetTarget()
	if target == nil {
		return Failure
	}

	pos := r.monster.GetPosition()
	currentRotation := r.monster.GetRotation()

	targetAngle := float32(math.Atan2(float64(target.Z-pos.Z), float64(target.X-pos.X)))
	angleDiff := math.Abs(float64(normalizeAngle(targetAngle-currentRotation))) * 180 / math.Pi

	// 이상 차이나면 애니메이션 회전이 필요
	if angleDiff > float64(r.viewAngle) {
		return Success // Success를 반환하여 애니메이션 회전 수행
	}

	return Failure // Failure를 반환하여 즉시 회전 후 이동
}
