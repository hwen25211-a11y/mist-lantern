# v1.1.0 验证记录

验证日期：2026-10-21。Unity 2022.3.62f3c1，Apple M1 Pro / 16 GB，macOS 发布版与内置浏览器 WebGL。全程静音；原生与编辑器使用 `-silent-test`，实际状态记录确认 `muted=true`，浏览器声音保持关闭。

本轮证据对应本地重建的 Keeper 主角、独立铜肩甲、提灯与背部装备，以及当前环境和战斗呈现。最终运行资源的 SHA256 在两种构建中完全一致。旧版记录保存在 [QA-v1.0.md](QA-v1.0.md)，不代替本轮验收。

## 模型与自动化

| 检查 | 结果 | 证据 |
| --- | --- | --- |
| 规则、存档和完整路线 | 44 条断言通过，含三武器 × 三种子 | [rules.txt](qa/rules.txt) |
| 动作专项 | 34 条断言通过 | [combat-v2.txt](qa/combat-v2.txt) |
| Unity Play Mode | 229 条断言，三种武器各自走完 12 个节点 | [play-results.txt](qa/play-results.txt) |
| 界面 | 54 次审查，文字溢出 0、运行错误 0 | 同上 |
| 蒙皮与关节 | 三种武器、六组攻击姿势、弯肘弯膝及披风摆动，有限顶点与权重归一化通过，边长拉伸未超门槛 | [mesh-checks.txt](qa/character-rebuild/mesh-checks.txt) |
| 全部模型和场景 | 材质、网格通道、面朝向、骨骼与引用通过 | [model-checks.txt](qa/sculpt-review/model-checks.txt) |
| DCC 与导出 | 原始形体、UV 图集、17 骨骼源文件及 10 个模型 OBJ/MTL | [角色来源](../ArtSource/Characters/README.md)、[model-export.txt](qa/sculpt-review/model-export.txt) |

Play Mode 通过实际 EventSystem 指针事件检查按钮，再使用正常移动、攻击、技能、药水等输入驱动模拟；固定测试步长为 0.05 秒。它属于自动化，不冒充下面的真实键鼠游玩。自动化还覆盖自然死亡、再次出发、暂停冻结、保存继续、永久升级和强制静音。

## macOS 真实游玩

使用最终发布应用，通过键盘实际完成 12 个节点和 3 位首领，种子 **1100700965**，引星灯杖，击败 57 个敌人；胜利生命 **135 / 164**，战斗计时 **01:37**，营地余烬 **231**。这次测试沿用独立 QA 档中的生命 2 级，初始最大生命 144；未修改运行中的生命、敌人或关卡状态。

在第一位首领的奖励节点正常退出进程，再启动并继续；恢复前后整份 Run JSON 完全相同。通关后在工坊升级生命 2 → 3，实际扣除 39 余烬（231 → 192）；新弯刀局最大生命从 144 → 156。实际补测了放灯、移动离开光区、跳跃、连击、格挡、闪避和明暗切换。

证据：[真实胜利截图](screenshots/character-rebuild/native-victory.png)、[胜利状态](qa/keeper-release/native-victory.json)、[完整流程](qa/keeper-release/native-flow.txt)、[重启一致性](qa/keeper-release/native-resume-check.json)、[升级及新局](qa/keeper-release/native-menu-check.json)、[实际战斗画面](screenshots/character-rebuild/native-gameplay.png)。

原生完整通关使用键盘。原生物理鼠标不列为已验证项；相同界面的 EventSystem 指针路径与 WebGL 发布版鼠标操作分别经过测试。

## WebGL 真实游玩

独立测试地址 `http://127.0.0.1:8921`，与常用地址 8920 的存档隔离。用鼠标购买生命与攻击升级，余烬 102 → 36；生命等级 1 → 2、攻击等级 2 → 3，分别扣费 27 和 39。

以种子 **1324792217** 开始新冒险，引星灯杖，完整走过 12 个节点和 3 位首领，第三节点主动选择险路并完成三波。最终击败 **64** 个敌人，胜利生命 **136 / 164**，战斗计时 **01:22**，11 个遗物，营地余烬 **131**。

实际验证鼠标长按攻击、右键技能、鼠标奖励选择、药水、闪避、补给和遗物叠加。在第一位首领后刷新页面，恢复至同一奖励节点，生命 138 / 144、药水 3、时间 00:34 与刷新前一致。检查了常规 1280×720 浏览区域和 1920×1080 全屏；胜利布局完整，无文字裁切。控制台错误列表为空。

通关后另开重锤局（种子 797777837），实测移动、长按蓄力、释放攻击、技能与跳跃；随后停止战斗输入，让敌人自然击败角色，确认失败结算与余烬到账。点击再次出发后，第一节点、满生命 144 / 144、药水 3 与零计时正确重置。对应 [重锤画面](screenshots/character-rebuild/web-hammer.jpg)、[失败页](screenshots/character-rebuild/web-loss.jpg)、[失败后重开](screenshots/character-rebuild/web-after-loss-restart.jpg) 与 [补充流程](qa/keeper-release/web-controls-death.json)。

证据：[浏览器胜利](screenshots/character-rebuild/web-victory.jpg)、[最终战](screenshots/character-rebuild/web-final-battle.jpg)、[刷新恢复](screenshots/character-rebuild/web-restored.jpg)、[全屏](screenshots/character-rebuild/web-fullscreen.jpg)、[流程日志](qa/keeper-release/web-flow.json)、[错误列表](qa/keeper-release/web-errors.json)。

## 本轮修正与视觉检查

| 发现的问题 | 修正 |
| --- | --- |
| 写实/程序拼装人物偏离确认稿 | 以确认稿进行本地三维重建，保留真实网格、侧背视图与灰模检查 |
| 侧面重影、手心和披风串色 | 四个几何对齐视图、可见性判断、遮挡面修补、同网格烘焙 |
| 持灯与挥砍时网格拉丝 | 修正骨骼热权重的跨臂/腿泄漏，按实际边长平滑权重 |
| 铜肩甲随手臂融化 | 拆出有厚度的刚性肩甲，保留下层柔性袖子 |
| 肩甲薄边法线反向 | 内外壳交界采用硬边法线 |
| 提灯挂环埋入手套、背包装备悬离身体 | 调整真实装备的握持和贴身位置 |
| 编辑器截图与 OBJ 漏掉蒙皮身体 | 检查/导出包含 SkinnedMeshRenderer，并明确 BakeMesh 后取图 |
| 多轮生成累积废弃资源 | 按实际 prefab、场景与 Resources 依赖清理无引用生成资产 |

实际 Unity 模型视图位于 `screenshots/character-rebuild/unity-*.png`；`unity-clay.png` 可直接检查去掉绘制图集后的真实形体。确认稿、绘制参考、编辑器渲染与发布版截图分别记录，不相互替代。制作与 AI 辅助来源见 [角色说明](../ArtSource/Characters/README.md)。

## 构建、性能与交付核验

两种构建均成功，无 C# 编译或 Shader 错误。`keeper-release/mac-build.json` 与 `web-build.json` 记录每个运行资源及实际构建文件的 SHA256；两者的运行资源身份一致。`Tools/verify_release.py` 对照这些摘要、最新通关证据和 HTTP 实际内容。`Tools/package.py` 先执行该核验，再检查压缩包 CRC、逐文件 SHA256 和 Mac 可执行权限。

原生重启后的实测段平均 **58.5 FPS**、内存约 **156 MB**；最大记录帧 432 ms，首次启动段最大 617 ms。WebGL 实测段平均 **59.8 FPS**、内存约 **115 MB**，最大记录帧 108 ms。统计包含加载和界面切换，不表示每帧恒定 60 FPS。

测试覆盖上述设备、构建与操作；没有将未实测平台写成已通过。当前已定位的运行错误和严重变形均修复并复查。最终交付校验见 [release-verification.json](qa/keeper-release/release-verification.json)。
