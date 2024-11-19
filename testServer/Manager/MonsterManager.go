package manager

import (
	"math"
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

func (mm *MonsterManager) AddMonster(id int32) *Monster {
	path := make([]common.Point, 0)

	// 1. 플레이어 위치 가져오기
	playerPoints := GetPlayerManager().ListPoints()
	initialRotation := float32(0)

	// 2. 플레이어가 있으면 첫 번째 플레이어 방향으로 초기 회전 설정
	if len(playerPoints) > 0 {
		firstPlayer := playerPoints[0]
		// 몬스터의 초기 위치
		monsterPos := common.Point{X: 10, Z: 10} // 초기 위치값

		// 플레이어 방향으로의 각도 계산
		dx := firstPlayer.X - monsterPos.X
		dz := firstPlayer.Z - monsterPos.Z
		initialRotation = float32(math.Atan2(float64(dz), float64(dx)))
	}

	// 3. 몬스터 생성 시 초기 회전값 전달
	monster := NewMonster(0, 10, 10, 100, path)
	monster.SetRotation(initialRotation) // 초기 회전값 설정

	mm.monsters[id] = monster
	mm.nextID++

	// 4. 스폰 메시지에 회전값 포함
	MonsterSapwn := &pb.GameMessage{
		Message: &pb.GameMessage_SpawnMonster{
			SpawnMonster: &pb.SpawnMonster{
				X:         monster.X,
				Z:         monster.Z,
				MonsterId: int32(monster.ID),
				// 프로토콜 버퍼 메시지에 회전값 필드 추가 필요
				// RotationY:  initialRotation,  // GameMessage.proto에 필드 추가 필요
			},
		},
	}

	for _, p := range GetPlayerManager().players {
		response := GetNetManager().MakePacket(MonsterSapwn)
		(*p.Conn).Write(response)
	}

	return monster
}
