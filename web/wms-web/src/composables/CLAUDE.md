# composables

## 文件
- `useReturnWorkbench.ts`: 退货工作台状态机，收敛装载、执行、审批与轨迹刷新。

## 原则
- composable 负责状态迁移，不负责协议细节。
- 页面只消费状态和动作，不自己拼业务流程。
