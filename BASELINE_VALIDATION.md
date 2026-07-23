# 第 1 天基线验证记录

> 基线提交：`bbbb5de`（执行前 `main` 与 `origin/main` 一致）
>
> 验证日期：2026-07-23
>
> 当前结论：**完成，360 秒 Development Player 主循环冒烟通过**

## 环境与发布目标

- 项目声明版本：Unity `2022.3.44f1 (c3ae09b9f03c)`。
- 本机编辑器版本：Unity `2022.3.44f1`，与项目完全一致。
- 1.0 主发布目标：Windows x64；本机已安装 `WindowsStandaloneSupport`。
- 开发验证平台：macOS。
- 参考分辨率：640×360 Pixel Perfect；默认窗口：1920×1080。
- Build Settings 只启用 `Assets/_Game/Scenes/Main.unity`。
- Unity Test Framework `1.1.33` 已安装。
- 许可证状态：Unity Hub 中的 Personal 许可证有效。命令行构建必须连接 Hub 正在运行的 Licensing Client；验证脚本会自动发现并传入该 IPC 通道。

## 冻结的可玩基线

当前主循环保持不变：

1. 以 300 金币、3 只幼虫、2 枚蛋开始；
2. 虫卵孵化、幼虫成年、自由成虫繁殖；
3. 建造工厂、兵营、医院和研究院，分配成虫并生产资源；
4. 普通敌人 90 秒后出现、每 45 秒一波，约 315 秒首次出现重型敌人；
5. 敌人攻击虫群或设施，最后一只虫死亡时进入 Colony Lost；
6. 当前完整建设、防御与三级工厂体验目标为 6–10 分钟。

## 可重复验证入口

- 终端入口：`Tools/run-baseline-smoke.sh`。
- Unity 菜单入口：`TCC > Validation > Build Baseline Smoke Player`。
- 默认测试时长：360 秒，可用 `TCC_SMOKE_SECONDS` 调整预检时长。
- 测试在临时项目副本中构建 macOS Development Player，不改动工作区场景。
- 只有显式传入 `-tccBaselineSmoke` 时，内置探针才会运行；正常启动不改变游戏流程。
- 探针验证开局资源、必要 Manager、时间推进、工厂生产、工人、士兵、普通/重型敌兵波次以及运行期错误，成功时输出 `[TCC Baseline] PASS`。

## 本次验证结果

- C# 玩家程序集：通过，0 error；保留 62 个既有的 Unity 序列化字段静态警告。
- C# 编辑器程序集：通过，0 error、0 warning。
- 场景静态检查：`Main.unity` 存在且为唯一启用场景；核心 Manager 均已接线。
- Windows x64 支持：安装目录存在。
- Unity 许可证/构建：通过；使用 Hub Licensing Client 成功生成 macOS Development Player。
- 12 秒预检：通过；开局资源、场景接线、构建和无界面启动正常。
- 360 秒主循环冒烟：通过；游戏内 Playing 累计 345 秒，峰值 14 只虫、6 枚蛋、8 个敌人。
- 完整玩法观察：重型敌人、工厂产物、工人、士兵均出现；Colony Lost 路径触发并由测试探针恢复 2 次；运行期错误 0。

## 已知基线问题

- 尚无正式档案、自动保存、存档迁移和损坏回退；这些属于第 2–6 天。
- 世界观演出、新手引导、三个纪元、胜利与反转尚未实现。
- 10–15 分钟后敌人与事件组合开始重复。
- 高级士兵动画、第二种普通敌人、重型敌人完整动画、三级建筑外观和 10 分钟事件/Boss 仍待补齐。
- `ProjectSettings.asset` 中公司名仍为 `DefaultCompany`，发布候选前需要锁定正式发行信息。
- 当前代码的既有静态编译警告主要来自场景/Prefab 注入的序列化字段；不阻塞编译，但应在后续测试基线中持续观察是否新增。

## 后续复验步骤

1. 保持 Unity Hub 已登录并运行。
2. 在仓库根目录运行 `Tools/run-baseline-smoke.sh`。
3. 确认开发构建成功、运行 360 秒并输出 `[TCC Baseline] PASS`。
