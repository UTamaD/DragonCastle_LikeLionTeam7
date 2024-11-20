package main

import (
	"encoding/binary"
	"fmt"
	"log"
	"net"

	pb "testServer/Messages"

	mg "testServer/Manager"

	"google.golang.org/protobuf/proto"
)

func main() {

	listener, err := net.Listen("tcp", ":9090")
	if err != nil {
		log.Fatalf("Failed to listen: %v", err)
	}
	defer listener.Close()
	fmt.Println("Server is listening on :9090")

	mg.GetMonsterManager().AddMonster(0)
	go mg.GetMonsterManager().UpdateMonster()

	for {
		conn, err := listener.Accept()
		if err != nil {
			log.Printf("Failed to accept connection: %v", err)
			continue
		}

		go handleConnection(conn)
	}
}

func handleConnection(conn net.Conn) {
	defer conn.Close()
	for {

		lengthBuf := make([]byte, 4)
		_, err := conn.Read(lengthBuf)
		if err != nil {
			log.Printf("Failed to read message length: %v", err)
			return
		}
		length := binary.LittleEndian.Uint32(lengthBuf)

		messageBuf := make([]byte, length)
		_, err = conn.Read(messageBuf)
		if err != nil {
			log.Printf("Failed to read message body: %v", err)
			return
		}

		message := &pb.GameMessage{}
		err = proto.Unmarshal(messageBuf, message)
		if err != nil {
			log.Printf("Failed to unmarshal message: %v", err)
			continue
		}

		processMessage(message, &conn)

	}
}

func processMessage(message *pb.GameMessage, conn *net.Conn) {
	switch msg := message.Message.(type) {
	case *pb.GameMessage_PlayerPosition:
		mg.GetPlayerManager().MovePlayer(msg)
	case *pb.GameMessage_Login:
		playerId := msg.Login.PlayerId
		playerManager := mg.GetPlayerManager()
		playerManager.AddPlayer(playerId, 0, conn)
	case *pb.GameMessage_Logout:
		playerId := msg.Logout.PlayerId
		playerManager := mg.GetPlayerManager()
		playerManager.RemovePlayer(playerId)
	case *pb.GameMessage_MonsterDamage:
		monsterManager := mg.GetMonsterManager()
		if monster, exists := monsterManager.GetMonster(msg.MonsterDamage.MonsterId); exists {
			monster.TakeDamage(
				int(msg.MonsterDamage.Damage),
				msg.MonsterDamage.HitPointX,
				msg.MonsterDamage.HitPointY,
				msg.MonsterDamage.HitPointZ,
				msg.MonsterDamage.HitNormalX,
				msg.MonsterDamage.HitNormalY,
				msg.MonsterDamage.HitNormalZ,
				msg.MonsterDamage.HitEffectType,
			)
		}
	case *pb.GameMessage_PlayerDamage:
		playerManager := mg.GetPlayerManager()
		playerManager.Broadcast(message)

	default:
		panic(fmt.Sprintf("unexpected messages.isGameMessage_Message: %#v", msg))
	}
}
