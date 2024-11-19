package manager

import (
	pb "testServer/Messages"
	"testServer/common"
	"time"
)

var monsterManager *MonsterManager

// PlayerManager manages a list of players
type MonsterManager struct {
	monsters map[int32]*Monster
	nextID   int32
}

// NewPlayerManager creates a new PlayerManager
func GetMonsterManager() *MonsterManager {
	if monsterManager == nil {
		monsterManager = &MonsterManager{
			monsters: make(map[int32]*Monster),
			nextID:   1,
		}
	}

	return monsterManager
}

func (mm *MonsterManager) UpdateMonster() {

	ticker := time.NewTicker(100 * time.Millisecond)
	for {
		select {
		case <-ticker.C:
			for _, m := range mm.monsters {
				m.AI.Execute()
			}
		}
	}
}

func (mm *MonsterManager) GetMonsters() []*Monster {
	monsters := make([]*Monster, 0, len(mm.monsters))
	for _, monster := range mm.monsters {
		monsters = append(monsters, monster)
	}
	return monsters
}

func (mm *MonsterManager) AddMonster(id int32) *Monster {
	path := make([]common.Point, 0)

	monster := NewMonster(0, 0, 0, 100, path)

	mm.monsters[id] = monster
	mm.nextID++

	MonsterSapwn := &pb.GameMessage{
		Message: &pb.GameMessage_SpawnMonster{
			SpawnMonster: &pb.SpawnMonster{
				X:         monster.X,
				Z:         monster.Z,
				MonsterId: int32(monster.ID),
			},
		},
	}

	for _, p := range GetPlayerManager().players {

		response := GetNetManager().MakePacket(MonsterSapwn)
		(*p.Conn).Write(response)
	}

	return monster
}
