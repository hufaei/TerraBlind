# 能力边界

这份清单描述当前代码的产品边界。游戏内能力不等于 HTTP 接口；HTTP 只负责状态和
Jev 日志观测。

## 保留的游戏内能力

| 能力 | 主要实现 | 说明 |
|---|---|---|
| 状态快照 | `Perception/StateSnapshotPlayer.cs`、`StateSerializer.cs` | 供游戏逻辑、Jev 和 `/state` 使用 |
| 物理模拟 | `Perception/PhysicsSimulator.cs` | 为寻路候选预测移动结果 |
| 寻路 | `Nav/RecedingNav.cs`、`StateSpacePlanner.cs` | 鼠标指向目标后按 `K` 启停；不提供 HTTP 控制端点 |
| 自动清障 | `RecedingNav.SmashPot` / `SmashWeb` | 赶路时自动砸罐子和清蛛网，继续保留 |
| 动作执行 | `Actions/`、`Core/ActExecutor.cs` | 移动、跳跃、挖掘和放置的内部执行器 |
| 生存反射 | `Flow/SurvivalReflex.cs` | 自动处理即时危险；不提供远程触发入口 |
| 导航风险与卡住分诊 | `Jev/Nav/` | 确定性判据和观测日志；当前不请求模型 |
| Jev boss 战 | `Jev/Fight/` | 战斗意图、能力检查、boss 背板和执行 |
| Jev 可观测性 | `JevHud`、`JevLog`、`JevPage` | HUD、环形日志和本地页面 |

## Decision Infra 契约

TerraBlind 只调用 Decision Infra 的 Jev 兼容入口：

```text
POST http://127.0.0.1:8080/v1/systemone
```

- 默认路由 ID：`jev-latest`。
- 可选本地路由 ID：`laya-multilingual`。
- URL 和模型来自模组配置。
- TypeSafe key 和模型权重由 gateway 管理，不能进入模组或 `.tmod`。
- `DECISION_GATEWAY_TOKEN` 只用于要求 Bearer token 的自建 gateway 或反向代理；
  默认 loopback Decision Infra 不需要它。

## 完整 HTTP API

监听地址固定为 `http://127.0.0.1:17878`。

| 方法 | 路径 | 响应 |
|---|---|---|
| `GET` | `/health` | `{"ok":true}` |
| `GET` | `/state` | 最新 `Snapshot` 的 JSON 序列化 |
| `GET` | `/jev` | 单文件 HTML 日志页面 |
| `GET` | `/jev_log` | `{"count":N,"entries":[...]}` |
| `POST` | `/jev_clear` | 清空日志并返回 `{"ok":true}` |

只有以上方法与路径组合有效：未知路径返回 `404`，错误方法返回 `405`。
没有 WebSocket、通用动作、流程触发、传送、背包、建造或调试 HTTP 接口。

## 已删除

| 旧编号 | 已删除范围 |
|---|---|
| C8 | 地下、丛林和地狱路线搜索及预览 |
| D6 | NPC 房屋自动建造 |
| D7 | 地狱长桥规划与建造 |
| D8 | 建筑录制与回放 |
| F1–F8 | 从新世界开局到肉山的完整通关流程 |
| G6–G10 | Agent 控制、流程触发和相关调试面 |

对应的 `/start_run`、`/hell_run`、`/find_descent`、`/descent_route`、
`/build_rec_start`、`/build_replay_start` 以及其他旧控制端点不再存在。

## 明确保留

自动砸罐子和清蛛网不属于已删除的建筑/通关流程。它们仍是
`RecedingNav` 的内部寻路清障行为，不需要也不提供 HTTP 端点。
