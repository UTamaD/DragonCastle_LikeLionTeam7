package behavior

import (
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
}

const (
	NoAttack = iota
	MeleeAttackType
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
	//log.Printf("Selector: All children failed, selector failing")
	return Failure
}

type Random struct {
	probability   float32
	option1       Node
	option2       Node
	lastChange    time.Time
	minInterval   time.Duration
	currentChoice int32
}

func NewRandom(probability float32, option1, option2 Node) *Random {
	return &Random{
		probability: probability,
		option1:     option1,
		option2:     option2,
		minInterval: 2 * time.Second, // 최소 2초 간격으로 변경
	}
}

func (r *Random) Execute() Status {
	now := time.Now()
	if now.Sub(r.lastChange) < r.minInterval {
		if r.currentChoice == 1 {
			return r.option1.Execute()
		}
		return r.option2.Execute()
	}

	r.lastChange = now
	if rand.Float32() < r.probability {
		r.currentChoice = 1
		log.Printf("Random: Choosing option 1 (probability: %.2f)", r.probability)
		return r.option1.Execute()
	}
	r.currentChoice = 2
	log.Printf("Random: Choosing option 2 (probability: %.2f)", r.probability)
	return r.option2.Execute()
}

type Wait struct {
	duration  time.Duration
	startTime time.Time
	started   bool
	state     *AttackState // 공유 상태
}

func NewWait(duration time.Duration, resetOnFailure bool, state *AttackState) *Wait { // state 매개변수 추가
	return &Wait{
		duration: duration,
		started:  false,
		state:    state,
	}
}

func (w *Wait) Execute() Status {
	if !w.started {
		w.startTime = time.Now()
		w.started = true
		return Running
	}

	if time.Since(w.startTime) >= w.duration {
		w.started = false
		w.state.mutex.Lock()
		w.state.currentAttack = NoAttack // Wait 완료 후 공격 상태 초기화
		w.state.mutex.Unlock()
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
		//log.Printf("Monster[%d] DetectPlayer: No target found", d.monster.GetID())
		return Failure
	}

	pos := d.monster.GetPosition()
	dist := distance(pos.X, pos.Z, target.X, target.Z)

	if dist <= d.range_ {
		//log.Printf("Monster[%d] DetectPlayer: Target found within range (%.2f)", d.monster.GetID(), dist)
		return Success
	}
	//log.Printf("Monster[%d] DetectPlayer: Target out of range (%.2f)", d.monster.GetID(), dist)
	return Failure
}

type AttackType int

const (
	MeleeAttack AttackType = iota
	RangedAttack
	MeteorAttack
)

type Attack struct {
	monster    common.IMonster
	range_     float32
	damage     int
	cooldown   time.Duration
	p          common.IPlayerManager
	n          common.INetworkManager
	attackType int32
	state      *AttackState
}

func NewAttack(monster common.IMonster, range_ float32, damage int, cooldown time.Duration,
	p common.IPlayerManager, n common.INetworkManager, attackType int32, state *AttackState) *Attack { // state 매개변수 추가
	return &Attack{
		monster:    monster,
		range_:     range_,
		damage:     damage,
		cooldown:   cooldown,
		p:          p,
		n:          n,
		attackType: attackType,
		state:      state,
	}
}
func (a *Attack) Execute() Status {

	a.state.mutex.Lock()
	defer a.state.mutex.Unlock()

	// 다른 공격이 진행 중인지 확인
	if a.state.currentAttack != NoAttack && a.state.currentAttack != a.attackType {
		return Failure
	}

	// 쿨다운 체크
	if time.Since(a.state.lastAttackTime) < a.cooldown {
		return Running
	}

	if a.monster.IsDead() {
		log.Printf("Monster[%d] Attack failed: monster is dead", a.monster.GetID())
		return Failure
	}

	target := a.monster.GetTarget()
	if target == nil {
		log.Printf("Monster[%d] Attack failed: no target", a.monster.GetID())
		return Failure
	}

	pos := a.monster.GetPosition()
	dist := distance(pos.X, pos.Z, target.X, target.Z)
	if dist > a.range_ {
		return Failure
	}

	now := time.Now()
	if now.Sub(a.state.lastAttackTime) < a.cooldown {
		return Running
	}

	log.Printf("Monster[%d] Performing attack type: %d", a.monster.GetID(), a.attackType)

	a.state.currentAttack = a.attackType

	// 원거리 공격인 경우 투사체 발사
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
	a.state.lastAttackTime = time.Now()

	return Success
}

// 구체적인 공격 타입들
type MeleeAttackNode struct {
	*Attack
}

func NewMeleeAttack(monster common.IMonster, range_ float32, damage int,
	cooldown time.Duration, p common.IPlayerManager, n common.INetworkManager, state *AttackState) *MeleeAttackNode {
	return &MeleeAttackNode{
		Attack: NewAttack(monster, range_, damage, cooldown, p, n, int32(MeleeAttack), state),
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
	monster common.IMonster
	speed   float32
	p       common.IPlayerManager
	n       common.INetworkManager
}

func NewChase(monster common.IMonster, speed float32, p common.IPlayerManager, n common.INetworkManager) *Chase {
	return &Chase{monster: monster, speed: speed, p: p, n: n}
}

func (c *Chase) Execute() Status {
	target := c.monster.GetTarget()
	if target == nil {
		return Failure
	}

	pos := c.monster.GetPosition()
	dx := target.X - pos.X
	dy := target.Z - pos.Z
	dist := float32(math.Sqrt(float64(dx*dx + dy*dy)))

	if dist < 0.1 {
		return Success
	}

	newX := pos.X + (dx/dist)*0.1
	newZ := pos.Z + (dy/dist)*0.1
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
	return Running
}

type MeteorAttackNode struct {
	monster  common.IMonster
	range_   float32
	damage   int
	cooldown time.Duration
	p        common.IPlayerManager
	n        common.INetworkManager
	state    *AttackState // AttackState 추가
}

func NewMeteorAttack(monster common.IMonster, range_ float32, damage int,
	cooldown time.Duration, p common.IPlayerManager, n common.INetworkManager, state *AttackState) *MeteorAttackNode {
	return &MeteorAttackNode{
		monster:  monster,
		range_:   range_,
		damage:   damage,
		cooldown: cooldown,
		p:        p,
		n:        n,
		state:    state,
	}
}

func (m *MeteorAttackNode) Execute() Status {
	m.state.mutex.Lock()
	defer m.state.mutex.Unlock()
	if m.monster.IsDead() {
		return Failure
	}

	target := m.monster.GetTarget()
	if target == nil {
		return Failure
	}

	if m.state.currentAttack != NoAttack && m.state.currentAttack != int32(MeteorAttack) {
		return Failure
	}

	if time.Since(m.state.lastAttackTime) < m.cooldown {
		return Running
	}

	monsterPos := m.monster.GetPosition()
	meteorPositions := make([]*pb.MeteorStrikePosition, 5)
	for i := 0; i < 5; i++ {
		// 랜덤 각도와 거리로 위치 생성
		angle := rand.Float64() * 2 * math.Pi
		distance := rand.Float32() * 40 // 100 유닛 범위 내

		x := monsterPos.X + distance*float32(math.Cos(angle))
		z := monsterPos.Z + distance*float32(math.Sin(angle))

		meteorPositions[i] = &pb.MeteorStrikePosition{
			X: x,
			Z: z,
		}
	}

	// 메테오 스트라이크 메시지 전송
	meteorMsg := &pb.GameMessage{
		Message: &pb.GameMessage_MeteorStrike{
			MeteorStrike: &pb.MeteorStrike{
				MonsterId: int32(m.monster.GetID()),
				Positions: meteorPositions,
			},
		},
	}
	m.p.Broadcast(meteorMsg)

	// 공격 메시지 전송
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

type RetreatAfterAttack struct {
	monster   common.IMonster
	speed     float32
	duration  time.Duration
	startTime time.Time
	started   bool
	p         common.IPlayerManager
	n         common.INetworkManager
}

func NewRetreatAfterAttack(monster common.IMonster, speed float32, duration time.Duration, p common.IPlayerManager, n common.INetworkManager) *RetreatAfterAttack {
	return &RetreatAfterAttack{
		monster:  monster,
		speed:    speed,
		duration: duration,
		p:        p,
		n:        n,
	}
}

func (r *RetreatAfterAttack) Execute() Status {

	if !r.started {
		r.startTime = time.Now()
		r.started = true
		log.Printf("Monster[%d] Retreat started", r.monster.GetID())
	}

	target := r.monster.GetTarget()
	if target == nil {
		return Failure
	}

	pos := r.monster.GetPosition()
	// 타겟으로부터 반대 방향 계산
	dx := pos.X - target.X
	dy := pos.Z - target.Z
	dist := float32(math.Sqrt(float64(dx*dx + dy*dy)))

	if dist > 0 {
		// 반대 방향으로 이동
		newX := pos.X + (dx/dist)*0.1*r.speed
		newZ := pos.Z + (dy/dist)*0.1*r.speed
		r.monster.SetPosition(newX, newZ)

		// 이동 메시지 전송
		moveMsg := &pb.GameMessage{
			Message: &pb.GameMessage_MoveMonster{
				MoveMonster: &pb.MoveMonster{
					X:         newX,
					Z:         newZ,
					MonsterId: int32(r.monster.GetID()),
				},
			},
		}
		r.p.Broadcast(moveMsg)
	}

	// 지정된 시간이 지나면 종료
	if time.Since(r.startTime) >= r.duration {
		r.started = false
		log.Printf("Monster[%d] Retreat completed", r.monster.GetID())
		return Success
	}
	return Running
}

type ApproachAfterAttack struct {
	monster   common.IMonster
	speed     float32
	duration  time.Duration
	startTime time.Time
	started   bool
	p         common.IPlayerManager
	n         common.INetworkManager
}

func NewApproachAfterAttack(monster common.IMonster, speed float32, duration time.Duration, p common.IPlayerManager, n common.INetworkManager) *ApproachAfterAttack {
	return &ApproachAfterAttack{
		monster:  monster,
		speed:    speed,
		duration: duration,
		p:        p,
		n:        n,
	}
}

func (a *ApproachAfterAttack) Execute() Status {
	if !a.started {
		a.startTime = time.Now()
		a.started = true
		log.Printf("Monster[%d] Approach started", a.monster.GetID())
	}

	target := a.monster.GetTarget()
	if target == nil {
		return Failure
	}

	pos := a.monster.GetPosition()
	dx := target.X - pos.X
	dy := target.Z - pos.Z
	dist := float32(math.Sqrt(float64(dx*dx + dy*dy)))

	if dist > 0 {
		// 타겟 방향으로 이동
		newX := pos.X + (dx/dist)*0.1*a.speed
		newZ := pos.Z + (dy/dist)*0.1*a.speed
		a.monster.SetPosition(newX, newZ)

		moveMsg := &pb.GameMessage{
			Message: &pb.GameMessage_MoveMonster{
				MoveMonster: &pb.MoveMonster{
					X:         newX,
					Z:         newZ,
					MonsterId: int32(a.monster.GetID()),
				},
			},
		}
		a.p.Broadcast(moveMsg)
	}

	if time.Since(a.startTime) >= a.duration {
		a.started = false
		log.Printf("Monster[%d] Approach completed", a.monster.GetID())
		return Success
	}
	return Running
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
