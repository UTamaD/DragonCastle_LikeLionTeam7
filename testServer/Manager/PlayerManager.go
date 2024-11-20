package manager

import (
	"errors"
	"net"

	pb "testServer/Messages"
	"testServer/common"
)

// 위치 정보를 담는 구조체
type Point struct {
	X, Y, Z float32
}

// Player represents a single player with some attributes
type Player struct {
	ID             int
	Name           string
	Age            int
	Conn           *net.Conn
	Point          Point
	RotationY      float32
	PlayerTemplate int32
}

var playerManager *PlayerManager

// PlayerManager manages a list of players
type PlayerManager struct {
	players map[string]*Player
	nextID  int
}

// NewPlayerManager creates a new PlayerManager
func GetPlayerManager() *PlayerManager {
	if playerManager == nil {
		playerManager = &PlayerManager{
			players: make(map[string]*Player),
			nextID:  1,
		}
	}

	return playerManager
}

func (pm *PlayerManager) Broadcast(sg *pb.GameMessage) {
	for _, p := range pm.players {

		response := GetNetManager().MakePacket(sg)

		(*p.Conn).Write(response)
	}
}

// AddPlayer adds a new player to the manager
func (pm *PlayerManager) AddPlayer(name string, age int, template int32, conn *net.Conn) *Player {
	player := Player{
		ID:             pm.nextID,
		Name:           name,
		Age:            age,
		Conn:           conn,
		Point:          Point{X: 0, Y: 0, Z: 0},
		RotationY:      0,
		PlayerTemplate: template,
	}

	pm.players[name] = &player
	pm.nextID++

	myPlayerSapwn := &pb.GameMessage{
		Message: &pb.GameMessage_SpawnMyPlayer{
			SpawnMyPlayer: &pb.SpawnMyPlayer{
				X:              player.Point.X,
				Y:              player.Point.Y,
				Z:              player.Point.Z,
				RotationY:      player.RotationY,
				PlayerTemplate: player.PlayerTemplate,
			},
		},
	}

	response := GetNetManager().MakePacket(myPlayerSapwn)
	if response == nil {
		return &player
	}

	(*player.Conn).Write(response)

	otherPlayerSpawnPacket := &pb.GameMessage{
		Message: &pb.GameMessage_SpawnOtherPlayer{
			SpawnOtherPlayer: &pb.SpawnOtherPlayer{
				PlayerId:       player.Name,
				X:              player.Point.X,
				Y:              player.Point.Y,
				Z:              player.Point.Z,
				RotationY:      player.RotationY,
				PlayerTemplate: player.PlayerTemplate,
			},
		},
	}

	response = GetNetManager().MakePacket(otherPlayerSpawnPacket)
	if response == nil {
		return &player
	}

	for _, p := range pm.players {
		if p.Name == name {
			continue
		}

		(*p.Conn).Write(response)
	}

	monsters := GetMonsterManager().GetMonsters()

	for _, monster := range monsters {
		MonsterSpawn := &pb.GameMessage{
			Message: &pb.GameMessage_SpawnMonster{SpawnMonster: &pb.SpawnMonster{X: monster.X, Z: monster.Z, MonsterId: int32(monster.ID), RotationY: monster.Rotation}},
		}
		response := GetNetManager().MakePacket(MonsterSpawn)
		(*player.Conn).Write(response)
	}

	for _, p := range pm.players {
		if p.Name == name {
			continue
		}

		otherPlayerSpawnPacket := &pb.GameMessage{
			Message: &pb.GameMessage_SpawnOtherPlayer{
				SpawnOtherPlayer: &pb.SpawnOtherPlayer{
					PlayerId:       p.Name,
					X:              p.Point.X,
					Y:              p.Point.Y,
					Z:              p.Point.Z,
					RotationY:      p.RotationY,
					PlayerTemplate: p.PlayerTemplate,
				},
			},
		}

		response = GetNetManager().MakePacket(otherPlayerSpawnPacket)

		(*player.Conn).Write(response)
	}

	return &player
}

func (pm *PlayerManager) SetPlayerApplyRootMotion(playerId string, rootMotion bool) {
	gameMessage := &pb.GameMessage{
		Message: &pb.GameMessage_ApplyRootMotion{
			ApplyRootMotion: &pb.ApplyRootMotion{
				PlayerId:   playerId,
				RootMosion: rootMotion,
			},
		},
	}

	response := GetNetManager().MakePacket(gameMessage)
	if response == nil {
		return
	}

	for _, player := range pm.players {
		if player.Name == playerId {
			continue
		}

		(*player.Conn).Write(response)
	}
}

func (pm *PlayerManager) SetPlayerAnimatorIntegerCondition(playerId string, animId string, condition int32) {
	gameMessage := &pb.GameMessage{
		Message: &pb.GameMessage_AnimatorSetInteger{
			AnimatorSetInteger: &pb.AnimatorSetInteger{
				PlayerId:  playerId,
				AnimId:    animId,
				Condition: condition,
			},
		},
	}

	response := GetNetManager().MakePacket(gameMessage)
	if response == nil {
		return
	}

	for _, player := range pm.players {
		if player.Name == playerId {
			continue
		}

		(*player.Conn).Write(response)
	}
}

func (pm *PlayerManager) SetPlayerAnimatorFloatCondition(playerId string, animId string, condition float32) {
	gameMessage := &pb.GameMessage{
		Message: &pb.GameMessage_AnimatorSetFloat{
			AnimatorSetFloat: &pb.AnimatorSetFloat{
				PlayerId:  playerId,
				AnimId:    animId,
				Condition: condition,
			},
		},
	}

	response := GetNetManager().MakePacket(gameMessage)
	if response == nil {
		return
	}

	for _, player := range pm.players {
		if player.Name == playerId {
			continue
		}

		(*player.Conn).Write(response)
	}
}

func (pm *PlayerManager) SetPlayerAnimatorBoolCondition(playerId string, animId string, condition bool) {
	gameMessage := &pb.GameMessage{
		Message: &pb.GameMessage_AnimatorSetBool{
			AnimatorSetBool: &pb.AnimatorSetBool{
				PlayerId:  playerId,
				AnimId:    animId,
				Condition: condition,
			},
		},
	}

	response := GetNetManager().MakePacket(gameMessage)
	if response == nil {
		return
	}

	for _, player := range pm.players {
		if player.Name == playerId {
			continue
		}

		(*player.Conn).Write(response)
	}
}

func (pm *PlayerManager) SetPlayerAnimatorTriggerCondition(playerId string, animId string) {
	gameMessage := &pb.GameMessage{
		Message: &pb.GameMessage_AnimatorSetTrigger{
			AnimatorSetTrigger: &pb.AnimatorSetTrigger{
				PlayerId: playerId,
				AnimId:   animId,
			},
		},
	}

	response := GetNetManager().MakePacket(gameMessage)
	if response == nil {
		return
	}

	for _, player := range pm.players {
		if player.Name == playerId {
			continue
		}

		(*player.Conn).Write(response)
	}
}

func (pm *PlayerManager) MovePlayer(p *pb.GameMessage_PlayerPosition) {

	pm.players[p.PlayerPosition.PlayerId].Point.X = p.PlayerPosition.X
	pm.players[p.PlayerPosition.PlayerId].Point.Y = p.PlayerPosition.Y
	pm.players[p.PlayerPosition.PlayerId].Point.Z = p.PlayerPosition.Z
	pm.players[p.PlayerPosition.PlayerId].RotationY = p.PlayerPosition.RotationY

	response := GetNetManager().MakePacket(&pb.GameMessage{
		Message: p,
	})
	if response == nil {
		return
	}

	for _, player := range pm.players {
		if player.Name == p.PlayerPosition.PlayerId {
			continue
		}

		(*player.Conn).Write(response)
	}
}

// GetPlayer retrieves a player by ID
func (pm *PlayerManager) GetPlayer(id string) (*Player, error) {
	player, exists := pm.players[id]
	if !exists {
		return nil, errors.New("player not found")
	}
	return player, nil
}

// RemovePlayer removes a player by ID
func (pm *PlayerManager) RemovePlayer(id string) error {
	if _, exists := pm.players[id]; !exists {
		return errors.New("player not found")
	}
	delete(pm.players, id)

	logoutPacket := &pb.GameMessage{
		Message: &pb.GameMessage_Logout{
			Logout: &pb.LogoutMessage{
				PlayerId: id,
			},
		},
	}

	response := GetNetManager().MakePacket(logoutPacket)

	// 이 코드를 들어온 유저를 제외한 플레이어들에게 스폰시켜달라고 한다.
	for _, p := range pm.players {
		(*p.Conn).Write(response)
	}

	return nil
}

// ListPlayers returns all players in the manager
func (pm *PlayerManager) ListPlayers() []*Player {
	playerList := []*Player{}
	for _, player := range pm.players {
		playerList = append(playerList, player)
	}
	return playerList
}

// ListPlayers returns all players in the manager
func (pm *PlayerManager) ListPoints() []*common.Point {
	playerList := []*common.Point{}
	for playerId, player := range pm.players {
		point := &common.Point{
			X:        player.Point.X,
			Z:        player.Point.Z,
			PlayerId: playerId, // PlayerId 설정
		}
		playerList = append(playerList, point)
	}
	return playerList
}
