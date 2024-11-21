// manager/monster.go
package manager

import (
	"fmt"
	pb "testServer/Messages"
	"testServer/behavior"
	"testServer/common"
	"time"
)

type Monster struct {
	ID        int
	X, Z      float32
	Rotation  float32
	Health    int
	MaxHealth int
	Target    *common.Point
	Path      []common.Point
	PathIndex int
	AI        behavior.Node
}

func NewMonster(id int, x, y float32, maxHealth int, path []common.Point) *Monster {
	m := &Monster{
		ID:        id,
		X:         x,
		Z:         y,
		Health:    maxHealth,
		MaxHealth: maxHealth,
		Path:      path,
		PathIndex: 0,
	}
	m.AI = CreateMonsterBehaviorTree(m)
	fmt.Printf("Monster Debug - Initial Position: (%f, %f)\n", m.X, m.Z) // 디버그 로그 추가

	return m
}
func (m *Monster) GetPosition() common.Point {
	fmt.Printf("Monster Debug - Getting Position: (%f, %f)\n", m.X, m.Z) // 디버그 로그 추가
	return common.Point{X: m.X, Z: m.Z}
}

func (m *Monster) SetPosition(x, y float32) {
	fmt.Printf("Monster Debug - Setting Position: (%f, %f)\n", x, y) // 디버그 로그 추가
	m.X = x
	m.Z = y
}

func (m *Monster) SetTarget(target *common.Point) {
	m.Target = target
}

func (m *Monster) GetTarget() *common.Point {
	if m.Target == nil {
		fmt.Println("Monster Debug - No target set")
		return nil
	}
	fmt.Printf("Monster Debug - Current Target: (%f, %f)\n", m.Target.X, m.Target.Z)
	return m.Target
}
func (m *Monster) GetPath() []common.Point {
	return m.Path
}

func (m *Monster) GetPathIndex() int {
	return m.PathIndex
}

func (m *Monster) SetPathIndex(idx int) {
	m.PathIndex = idx
}

func (m *Monster) GetID() int {
	return m.ID
}

func (m *Monster) GetHealth() int {
	return m.Health
}

func (m *Monster) SetHealth(health int) {
	m.Health = health
	if m.Health < 0 {
		m.Health = 0
	}
	if m.Health > m.MaxHealth {
		m.Health = m.MaxHealth
	}
}

func (m *Monster) GetMaxHealth() int {
	return m.MaxHealth
}

func (m *Monster) IsDead() bool {
	return m.Health <= 0
}

func (m *Monster) GetRotation() float32 {
	return m.Rotation
}

func (m *Monster) SetRotation(r float32) {
	m.Rotation = r
}

func (m *Monster) Update() {
	if !m.IsDead() {
		m.AI.Execute()
	}
}

func (mm *MonsterManager) GetMonster(id int32) (*Monster, bool) {
	monster, exists := mm.monsters[id]
	return monster, exists
}

func (m *Monster) TakeDamage(damage int, hitPointX, hitPointY, hitPointZ, hitNormalX, hitNormalY, hitNormalZ float32, hitEffectType int32) {
	m.Health -= damage
	if m.Health < 0 {
		m.Health = 0
	}

	damageMsg := &pb.GameMessage{
		Message: &pb.GameMessage_MonsterDamage{
			MonsterDamage: &pb.MonsterDamage{
				MonsterId: int32(m.ID),
				Damage:    float32(damage),
				CurrentHp: int32(m.Health),
			},
		},
	}

	hitEffectMsg := &pb.GameMessage{
		Message: &pb.GameMessage_MonsterHitEffect{
			MonsterHitEffect: &pb.MonsterHitEffect{
				MonsterId: int32(m.ID),
				HitPoint: &pb.Point3D{
					X: hitPointX,
					Y: hitPointY,
					Z: hitPointZ,
				},
				HitNormal: &pb.Point3D{
					X: hitNormalX,
					Y: hitNormalY,
					Z: hitNormalZ,
				},
				HitEffectType: hitEffectType,
			},
		},
	}

	GetPlayerManager().Broadcast(damageMsg)
	GetPlayerManager().Broadcast(hitEffectMsg)
}

func CreateMonsterBehaviorTree(monster common.IMonster) behavior.Node {
	// Get manager instances
	playerManager := GetPlayerManager()
	netManager := GetNetManager()

	// Initialize states for attack patterns and action management
	state := &behavior.AttackState{}
	patternState := &behavior.PatternState{MaxRepeat: 2}
	actionState := behavior.NewActionState()

	// Define chase sequence with animated rotation for large angle differences (>45 degrees)
	chaseWithAnimRotation := behavior.NewSequence(
		behavior.NewDetectPlayer(monster, 50.0, playerManager, netManager),
		behavior.NewRotateToTarget(monster, 2.0, playerManager, netManager),
		behavior.NewChase(monster, 2.0, playerManager, netManager, actionState),
	)

	// Define chase sequence with instant rotation for small angle differences
	chaseWithInstantRotation := behavior.NewSequence(
		behavior.NewDetectPlayer(monster, 50.0, playerManager, netManager),
		behavior.NewRotateWithoutAnimation(monster, playerManager, netManager),
		behavior.NewChase(monster, 2.0, playerManager, netManager, actionState),
	)

	// Create a chase selector that chooses between animated and instant rotation based on angle difference
	chaseSelector := behavior.NewSequence(
		behavior.NewDetectPlayer(monster, 50.0, playerManager, netManager),
		behavior.NewSelector(
			// If angle difference > 45 degrees, use animated rotation
			behavior.NewSequence(
				behavior.NewRotationCheckBeforeChase(monster),
				chaseWithAnimRotation,
			),
			// Otherwise use instant rotation
			chaseWithInstantRotation,
		),
		behavior.NewWait(10*time.Second, false, state),
	)

	// Combat selector manages different attack patterns
	combatSelector := behavior.NewSelector(
		// Melee attack sequence - close range
		behavior.NewSequence(
			behavior.NewDetectPlayer(monster, 4.0, playerManager, netManager),
			behavior.NewRotateWithoutAnimation(monster, playerManager, netManager),
			behavior.NewMeleeAttack(monster, 4.0, 10, 4*time.Second, playerManager, netManager, state),
			behavior.NewWait(5*time.Second, false, state),
		),
		// Ranged/Meteor attack selector with mutual exclusion
		behavior.NewMutuallyExclusiveSelector(0.33,
			[]behavior.Node{
				// Ranged attack sequence - medium range
				behavior.NewSequence(
					behavior.NewDetectPlayer(monster, 30.0, playerManager, netManager),
					behavior.NewRotateWithoutAnimation(monster, playerManager, netManager),
					behavior.NewRangedAttack(monster, 30.0, 8, 10*time.Second, playerManager, netManager, state),
					behavior.NewWait(5*time.Second, false, state),
				),
				// Meteor attack sequence - long range
				behavior.NewSequence(
					behavior.NewDetectPlayer(monster, 40.0, playerManager, netManager),
					behavior.NewRotateWithoutAnimation(monster, playerManager, netManager),
					behavior.NewMeteorAttack(monster, 40.0, 15, 10*time.Second, playerManager, netManager, state),
					behavior.NewWait(5*time.Second, false, state),
				),
			},
		),
	)

	// Root node: Main behavior selector
	return behavior.NewSelector(
		behavior.NewSequence(
			// First check if player is in detection range
			behavior.NewDetectPlayer(monster, 50.0, playerManager, netManager),
			// Then use pattern tracker to manage combat and chase patterns
			behavior.NewPatternTracker(patternState, combatSelector, chaseSelector),
		),
	)
}
