// manager/monster.go
package manager

import (
	"testServer/behavior"
	"testServer/common"
	"time"
)

type Monster struct {
	ID        int
	X, Z      float32
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
	return m
}
func (m *Monster) GetPosition() common.Point {
	return common.Point{X: m.X, Z: m.Z}
}

func (m *Monster) SetPosition(x, y float32) {
	m.X = x
	m.Z = y
}

func (m *Monster) SetTarget(target *common.Point) {
	m.Target = target
}

func (m *Monster) GetTarget() *common.Point {
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

func (m *Monster) Update() {
	if !m.IsDead() {
		m.AI.Execute()
	}
}

// CreateMonsterBehaviorTree creates the AI behavior tree for the monster
func CreateMonsterBehaviorTree(monster *Monster) behavior.Node {
	playerManager := GetPlayerManager()
	netManager := GetNetManager()

	return behavior.NewSelector(
		// Combat sequence
		behavior.NewSequence(
			// Detect player (10 units range)
			behavior.NewDetectPlayer(monster, 50.0, playerManager, netManager),
			behavior.NewSelector(
				// Melee attack sequence (within 2 units)
				behavior.NewSequence(
					behavior.NewDetectPlayer(monster, 2.0, playerManager, netManager),
					behavior.NewMeleeAttack(monster, 2.0, 10, 2*time.Second, playerManager, netManager),
				),
				// Ranged attack sequence (2-8 units range)
				behavior.NewRandom(
					0.5,
					behavior.NewSequence(
						behavior.NewRangedAttack(monster, 50.0, 8, 13*time.Second, playerManager, netManager),
					),
					behavior.NewSequence(
						behavior.NewMeteorAttack(monster, 59.0, 15, 15*time.Second, playerManager, netManager),
					),
				),
				// Chase (when player is detected but outside attack range)
				behavior.NewChase(monster, 50.0, playerManager, netManager),
			),
		),
	)
}
