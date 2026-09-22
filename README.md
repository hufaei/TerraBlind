# TerraBlind

[中文](#中文) · [English](#english)

![tModLoader](https://img.shields.io/badge/tModLoader-1.4.4.9-blue)
![decision](https://img.shields.io/badge/decision-Jev%20%7C%20Laya-brightgreen)
![HTTP](https://img.shields.io/badge/HTTP-5%20local%20endpoints-lightgrey)

TerraBlind 是一个 tModLoader 实验模组：保留游戏内的感知、寻路、动作执行与
Jev boss 战，把原先面向外部 Agent 的大规模 HTTP 控制面收缩为观测用途和 Jev 日志。

TerraBlind is a tModLoader experiment that keeps in-game perception, navigation, action
execution, and Jev-driven boss combat while reducing the former agent-facing HTTP control
surface to observability-only endpoints and Jev logs.

---

## 中文

### 当前范围

Boss 走位最多每 200 ms 向 Decision Infra 提交一次现场，普通战斗只在局面明显变化时提交；
模型返回带概率的类型化判断，游戏内反射层把判断翻译成
瞄准、走位、跳跃、冲刺和钩爪操作。Jev 负责判断，代码负责时序、距离、能力检查和过期回退。

保留的核心能力：

- 玩家与世界状态快照和物理模拟。
- 游戏内寻路、挖掘、放置、移动与战斗执行器。
- 赶路时自动砸罐子和清蛛网。
- Jev boss 背板、战斗判断、HUD 和环形日志。
- 本地、仅观测用途的 HTTP 接口。

### Decision Infra

模型请求统一发送给 Decision Infra：

| 配置 | 默认值 |
|---|---|
| Gateway | `http://127.0.0.1:8080/v1/systemone` |
| Model | `jev-latest` |
| 本地模型选项 | `laya-multilingual` |

地址和模型可在模组配置中修改。TypeSafe API key 只放在 Decision Infra 进程中，
不进入模组配置、游戏存档或 `.tmod`。模组对 gateway 使用 Jev
`POST /v1/systemone` 契约；它不直连 TypeSafe，也不管理模型权重。

默认 loopback gateway 不需要入站 token。如果外部反向代理或自建 gateway 要求 Bearer token，
可通过游戏进程环境变量 `DECISION_GATEWAY_TOKEN` 提供。

### 本地 HTTP

进入世界后服务监听 `http://127.0.0.1:17878`。只支持以下 5 个端点：

| 方法 | 路径 | 用途 |
|---|---|---|
| `GET` | `/health` | 进程连通性检查 |
| `GET` | `/state` | 最新游戏状态快照 |
| `GET` | `/jev` | 自动刷新的 Jev 日志页面 |
| `GET` | `/jev_log` | Jev 日志 JSON |
| `POST` | `/jev_clear` | 清空 Jev 日志 |

未知路径返回 `404`，已知路径使用错误方法返回 `405`。服务只绑定 IPv4 loopback；请使用
`127.0.0.1`，不要使用可能解析到 `::1` 的 `localhost`。这个接口没有远程控制能力，也不应转发到公网。

```sh
curl http://127.0.0.1:17878/health
curl http://127.0.0.1:17878/state
open http://127.0.0.1:17878/jev
curl -X POST http://127.0.0.1:17878/jev_clear
```

### 已删除的范围

以下旧能力不再属于当前产品面，也不再通过 HTTP 暴露：

- C8：地下、丛林和地狱路线。
- D6：NPC 房屋自动建造。
- D7：地狱长桥。
- D8：建筑录制与回放。
- F1–F8：从新世界到肉山的完整通关编排。
- G6–G10：配套的 Agent 控制、流程触发和调试端点。

旧 `/start_run`、`/hell_run`、移动、建造、背包和 WebSocket 等 HTTP 入口均已删除。
自动砸罐子和清蛛网是游戏内寻路行为，继续保留，不依赖 HTTP。

### 安装与开发

运行要求：Terraria 1.4.4.9 与对应版本的 tModLoader。精简后的战斗模组不依赖 DragonLens。

1. 将仓库放进 `tModLoader/ModSources/TerraBlind/`。
2. 运行 `./check.sh` 做快速类型检查。
3. 在 tModLoader 的“创意工坊 → 开发模组”中构建并重新加载。

进入世界后，把鼠标移到目标地块并按 `K` 启动或停止基础寻路；按键可在 Terraria
控制设置中修改。砸罐子、清蛛网、挖掘和普通搭桥会由这条寻路链按现场需要调用。

关键配置：

- `FightBack`：允许自动战斗。
- `DodgeBoss`：允许 Jev boss 走位。
- `BossKnowledge`：启用 boss 背板。
- `ShowJevHud`：显示 Jev HUD。
- `DecisionGatewayUrl` / `DecisionModel`：选择 Decision Infra 路由。

完整能力边界见 [`CAPABILITIES.md`](CAPABILITIES.md)。

---

## English

### Current scope

Boss movement submits at most one snapshot every 200 ms; ordinary combat submits only when
the scene materially changes. The model returns typed, probabilistic decisions.
The in-game reflex layer turns those decisions into aiming, movement, jumps, dashes, and
grapples. The model decides; code owns timing, distance, capability checks, and expiry.

The retained core includes:

- Player/world snapshots and physics simulation.
- In-game navigation, mining, placement, movement, and combat executors.
- Automatic pot smashing and cobweb clearing while navigating.
- Jev boss playbooks, decisions, HUD, and ring log.
- A small local, observability-only HTTP surface.

### Decision Infra

All model requests go through Decision Infra:

| Setting | Default |
|---|---|
| Gateway | `http://127.0.0.1:8080/v1/systemone` |
| Model | `jev-latest` |
| Local model option | `laya-multilingual` |

The URL and model are editable in the mod configuration. The TypeSafe API key belongs only
to the Decision Infra process; it is never stored in the mod configuration, world save, or
`.tmod`. TerraBlind uses the Jev `POST /v1/systemone` contract and does not manage provider
credentials or model weights.

The default loopback gateway needs no inbound token. Set `DECISION_GATEWAY_TOKEN` in the
game process only when a reverse proxy or custom gateway requires a Bearer token.

### Local HTTP API

The server listens on `http://127.0.0.1:17878` after entering a world. Its complete API is:

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/health` | Process liveness |
| `GET` | `/state` | Latest game-state snapshot |
| `GET` | `/jev` | Auto-refreshing Jev log page |
| `GET` | `/jev_log` | Jev log as JSON |
| `POST` | `/jev_clear` | Clear the Jev log |

Unknown paths return `404`; a wrong method on a known path returns `405`. The listener is
IPv4 loopback-only, so use `127.0.0.1`, not `localhost`, which may resolve to `::1`. It has no
remote-control surface and must not be exposed to the public internet.

### Removed scope

The following legacy areas are no longer part of the current product surface:

- C8 underground, jungle, and underworld routing.
- D6 NPC-house automation.
- D7 long underworld bridge construction.
- D8 building recording and replay.
- F1–F8 full fresh-world-to-Wall-of-Flesh orchestration.
- G6–G10 agent control, workflow triggers, and related debug endpoints.

The old `/start_run`, `/hell_run`, movement, building, inventory, and WebSocket HTTP routes
are gone. Automatic pot smashing and cobweb clearing remain internal navigation behavior.

### Install and develop

Requirements are Terraria 1.4.4.9 and the matching tModLoader release. The trimmed combat
mod does not require DragonLens.

1. Put the repository in `tModLoader/ModSources/TerraBlind/`.
2. Run `./check.sh` for the fast type check.
3. Build and reload it from tModLoader's Workshop → Develop Mods screen.

In a world, point at a destination tile and press `K` to start or stop basic navigation;
the binding is editable in Terraria's controls. Pot smashing, cobweb clearing, mining, and
ordinary bridging are invoked by that navigation chain when the terrain requires them.

See [`CAPABILITIES.md`](CAPABILITIES.md) for the exact retained and removed capability boundary.

### License

MIT
