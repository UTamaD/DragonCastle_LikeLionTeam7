using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;


// StageBasedSelector: 단계별로 다른 행동을 선택하는 복합 행동 트리 노드
public class StageBasedSelector : Composite
{
    // 현재 보스의 단계를 나타내는 공유 변수
    public SharedInt CurrentStage;
    // 각 단계별로 포함된 작업들의 목록
    public List<string> IncludedTasksPerStage;
    
    // 디버깅을 위한 랜덤 시드 설정
    [UnityEngine.Tooltip("Seed the random number generator to make things easier to debug")]
    public int seed = 0;
    [UnityEngine.Tooltip("Do we want to use the seed?")]
    public bool useSeed = false;

    // Fischer-Yates 셔플 알고리즘을 위한 자식 작업 인덱스 리스트
    private List<int> childIndexList = new List<int>();
    // 무작위로 결정된 자식 작업 실행 순서
    private Stack<int> childrenExecutionOrder = new Stack<int>();
    // 마지막으로 실행된 자식 작업의 상태
    private TaskStatus executionStatus = TaskStatus.Inactive;

    // 초기화 시 호출되는 메서드
    public override void OnAwake()
    {
        // 지정된 경우 제공된 시드 사용
        if (useSeed) {
            Random.InitState(seed);
        }
    }

    // 작업 시작 시 호출되는 메서드
    public override void OnStart()
    {
        // 현재 단계에 기반하여 고려할 자식 작업 인덱스 선택
        childIndexList.Clear();
        childIndexList = IncludedTasksPerStage[CurrentStage.Value].Split(',').Select(int.Parse).ToList();
        
        // 선택된 자식 작업들의 순서를 무작위로 섞음
        ShuffleChilden();
    }

    // 현재 실행 중인 자식 작업의 인덱스를 반환
    public override int CurrentChildIndex()
    {
        // 스택의 맨 위 인덱스 반환
        return childrenExecutionOrder.Peek();
    }

    // 실행 가능 여부를 확인하는 메서드
    public override bool CanExecute()
    {
        // 실행할 작업이 남아있고 성공한 작업이 없을 때 계속 실행
        return childrenExecutionOrder.Count > 0 && executionStatus != TaskStatus.Success;
    }

    // 자식 작업 실행 후 호출되는 메서드
    public override void OnChildExecuted(TaskStatus childStatus)
    {
        // 실행된 작업을 스택에서 제거하고 상태 업데이트
        if (childrenExecutionOrder.Count > 0) {
            childrenExecutionOrder.Pop();
        }
        executionStatus = childStatus;
    }

    // 조건부 중단 시 호출되는 메서드
    public override void OnConditionalAbort(int childIndex)
    {
        // 중단 시 처음부터 다시 시작
        childrenExecutionOrder.Clear();
        executionStatus = TaskStatus.Inactive;
        ShuffleChilden();
    }

    // 작업 종료 시 호출되는 메서드
    public override void OnEnd()
    {
        // 모든 자식 작업 실행 후 변수 초기화
        executionStatus = TaskStatus.Inactive;
        childrenExecutionOrder.Clear();
    }

    // 작업 리셋 시 호출되는 메서드
    public override void OnReset()
    {
        // 공개 속성을 원래 값으로 리셋
        seed = 0;
        useSeed = false;
    }

    // 자식 작업들의 순서를 무작위로 섞는 메서드
    private void ShuffleChilden()
    {
        // Fischer-Yates 셔플 알고리즘을 사용하여 자식 인덱스 순서를 무작위로 섞음
        for (int i = childIndexList.Count; i > 0; --i) {
            int j = Random.Range(0, i);
            int index = childIndexList[j];
            childrenExecutionOrder.Push(index);
            childIndexList[j] = childIndexList[i - 1];
            childIndexList[i - 1] = index;
        }
    }
}
