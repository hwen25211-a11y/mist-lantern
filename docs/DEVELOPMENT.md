# 开发与协作

Unity 版本固定为 2022.3.62f3c1；用 Unity Hub 打开项目根目录。

## 代码分工

| 文件 | 职责 |
| --- | --- |
| Rules.cs | 随机种子、战斗、敌人、危险区域、房间流程、遗物、存档 |
| Models.cs | 原创可复现模型生成 |
| Presentation.cs | 场景、模型动作、打击反馈、声音、UI 基础组件 |
| Game.cs | 实际输入、界面与保存生命周期 |
| Editor/BuildGame.cs | 字体、Prefab、场景和构建 |
| Editor/RuleChecks.cs | 规则与多武器完整路线检查 |
| Editor/PlayChecks.cs | 实际 Unity 场景与 EventSystem 交互检查 |

## Git

main 保存经验证的里程碑。修改时创建 `feature/<功能>` 或 `fix/<问题>` 分支，提交功能和对应的验证记录。保留所有 Assets 的 .meta 文件；不提交 Library、Temp、Build、release、用户设置或账号凭证。源项目中的场景与 Prefab 使用文本序列化，便于审阅差异。

生成资源需要与生成代码一同提交。若改动界面文案，运行 prepare 更新中文图集后构建。只在真正修改模型时运行 models。

## 构建

```sh
Tools/build.sh prepare
Tools/build.sh checks
Tools/build.sh smoke
Tools/build.sh mac
Tools/build.sh web
python3 Tools/package.py
```

同一项目同时只运行一个 Unity 编辑器进程。工具始终传入 `-silent-test`；构建产物的用户默认也是静音，可在游戏内自行打开。

## 保存

常规保存键为 `MistLantern.Save.v1`，保留前一次有效保存作为备份。测试进程使用独立的 `MistLantern.QA.v1`，不覆盖用户冒险。

保存包含敌人、波次、预警、灯油、技能冷却、随机数状态与遗物。战斗中每八秒保存，房间和奖励切换时立即保存，暂停和退出也会保存。
