package behavior

import (
	"math"
	"math/rand/v2"
	pb "testServer/Messages"
	"testServer/common"
	"time"
)

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
	children []Node
}

func NewSequence(children ...Node) *Sequence {
	return &Sequence{children: children}
}

func (s *Sequence) Execute() Status {
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
	children []Node
}

func NewSelector(children ...Node) *Selector {
	return &Selector{children: children}
}

func (s *Selector) Execute() Status {
	for _, child := range s.children {
		switch child.Execute() {
		case Success:
			return Success
		case Running:
			return Running
		}
	}
	return Failure
}

type Random struct {
	probability float32
	option1     Node
	option2     Node
}

func NewRandom(probability float32, option1, option2 Node) *Random {
	return &Random{
		probability: probability,
		option1:     option1,
		option2:     option2,
	}
}

func (r *Random) Execute() Status {
	if rand.Float32() < r.probability {
		return r.option1.Execute()
	}
	return r.option2.Execute()
}

type Wait struct {
	duration  time.Duration
	startTime time.Time
	started   bool
}

func NewWait(duration time.Duration) *Wait {
	return &Wait{
		duration: duration,
		started:  false,
	}
}

func (w *Wait) Execute() Status {
	if !w.started {
		w.startTime = time.Now()
		w.started = true
	}

	if time.Since(w.startTime) >= w.duration {
		w.started = false
		return Success
	}
	return Running
}

type DetectPlayer struct {
	monster common.IMonster
	range_  float32
	p       common.IPlayerManager
	n       common.INetworkManager
}

func NewDetectPlayer(monster common.IMonster, range_ float32, p common.IPlayerManager, n common.INetworkManager) *DetectPlayer {
	return &DetectPlayer{
		monster: monster,
		range_:  range_,
		p:       p,
		n:       n,
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
	monster    common.IMonster
	range_     float32
	damage     int
	lastAttack time.Time
	cooldown   time.Duration
	p          common.IPlayerManager
	n          common.INetworkManager
	attackType int32
}

func NewAttack(monster common.IMonster, range_ float32, damage int, cooldown time.Duration, p common.IPlayerManager, n common.INetworkManager, attackType int32) *Attack {
	return &Attack{
		monster:    monster,
		range_:     range_,
		damage:     damage,
		cooldown:   cooldown,
		p:          p,
		n:          n,
		attackType: attackType,
	}
}

func (a *Attack) Execute() Status {
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

	now := time.Now()
	if now.Sub(a.lastAttack) < a.cooldown {
		return Running
	}

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
	a.lastAttack = now

	return Success
}

// 구체적인 공격 타입들
type MeleeAttackNode struct {
	*Attack
}

func NewMeleeAttack(monster common.IMonster, range_ float32, damage int, cooldown time.Duration, p common.IPlayerManager, n common.INetworkManager) *MeleeAttackNode {
	return &MeleeAttackNode{
		Attack: NewAttack(monster, range_, damage, cooldown, p, n, int32(MeleeAttack)),
	}
}

type RangedAttackNode struct {
	*Attack
}

func NewRangedAttack(monster common.IMonster, range_ float32, damage int, cooldown time.Duration, p common.IPlayerManager, n common.INetworkManager) *RangedAttackNode {
	return &RangedAttackNode{
		Attack: NewAttack(monster, range_, damage, cooldown, p, n, int32(RangedAttack)),
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

	newX := pos.X + (dx/dist)*0.01
	newZ := pos.Z + (dy/dist)*0.01
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
	monster    common.IMonster
	range_     float32
	damage     int
	lastAttack time.Time
	cooldown   time.Duration
	p          common.IPlayerManager
	n          common.INetworkManager
}

func NewMeteorAttack(monster common.IMonster, range_ float32, damage int, cooldown time.Duration, p common.IPlayerManager, n common.INetworkManager) *MeteorAttackNode {
	return &MeteorAttackNode{
		monster:  monster,
		range_:   range_,
		damage:   damage,
		cooldown: cooldown,
		p:        p,
		n:        n,
	}
}

func (m *MeteorAttackNode) Execute() Status {
	if m.monster.IsDead() {
		return Failure
	}

	target := m.monster.GetTarget()
	if target == nil {
		return Failure
	}

	now := time.Now()
	if now.Sub(m.lastAttack) < m.cooldown {
		return Running
	}

	// 5개의 랜덤 위치 생성
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

	m.lastAttack = now
	return Success
}

func distance(x1, y1, x2, y2 float32) float32 {
	dx := x2 - x1
	dy := y2 - y1
	return float32(math.Sqrt(float64(dx*dx + dy*dy)))
}
