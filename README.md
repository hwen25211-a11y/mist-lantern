# 雾灯遗城 · Mist Lantern

Unity 2022.3.62f3c1 / C#，中文单机 2.5D 动作 Rogue-lite。

当前 v0.1.0 为功能原型，美术、建模和动作表现尚未达到交付标准。重做前的参考游戏调研、现版差距与验收要求见 [失落城堡 2 调研](docs/research/LOST-CASTLE-2.md)。

扮演最后一名守灯人，穿过旧城桥、雾庭与机械钟楼，让停摆的钟再次响起。
明灯照破雾甲；暗灯回收灯油。你必须在进攻窗口与续航之间主动切换。

## 操作

WASD / 方向键移动，J / 鼠标左键连击，K / Shift 闪避，Space 跳跃，
L / 鼠标右键武器技，Q 切换灯态，F 喝药，E 进入下一房间，Esc 暂停。
可按住攻击键连续攻击。地图内有纵深，攻击需要靠近目标。

## 游玩

运行版本在 `Build/macOS/雾灯遗城.app` 和 `Build/WebGL`。
本地浏览器启动：`python3 Tools/preview.py`，地址固定为 http://127.0.0.1:8920 。
默认静音，可以在扉页或暂停菜单自行打开声音。

完整内容包括三种武器、三片区域、十二个房间节点、三位首领、十二种可叠加遗物、分波遭遇、险路、补给选择、三项营地成长和自动存档。

## 开发

用 Unity Hub 打开本目录。菜单「雾灯遗城」提供场景准备、规则检查和构建。
`Tools/build.sh prepare|mac|web|checks|smoke`。
场景和模型由可复现的编辑器工具生成，模型网格与 Prefab 保存到 Assets/Models。
源码、美术来源、测试与交付说明在 docs 下。

验证范围与发现问题的修复记录见 [QA.md](docs/QA.md)，美术与模型来源见 [ART-SOURCES.md](docs/ART-SOURCES.md)。
