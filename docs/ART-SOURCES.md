# 美术、模型与声音来源

## 可编辑的三维模型

原创几何造型生成代码为 `Assets/Scripts/Models.cs`。Unity 菜单「雾灯遗城 / 重新生成原创模型」生成网格、材质与 Prefab：

- `Assets/Models/Meshes`：实际三角网格，含倒角石材、兜帽、斗篷、武器、灯笼与齿轮。
- `Assets/Models/Materials`：统一配色的材质。
- `Assets/Resources/Models`：三种武器猎人、四类普通敌人与三位首领。
- `Assets/Resources/Stages`：旧桥、庭院、钟楼环境。
- 分部件动作逻辑在 `Presentation.cs`，可在模型层级中编辑 Head、ArmL、ArmR、LegL、LegR 和 Weapon。
- `Assets/Scenes/ModelGallery.unity` 是单独的模型陈列场景，编辑器中无需运行游戏即可查看三种猎人、四类普通敌人与三位首领。

模型不是由标题插画自动转换。建模方式是原创程序化几何与部件组装，动作是程序驱动，并非外包手工雕刻或第三方绑定动画。用于课程展示时应如实注明。

## 标题插画

文件：`Assets/Resources/Art/Title.png`。
使用内置 imagegen 生成，未使用外部游戏原图。该图仅用于标题画面与美术方向参考，游戏战斗画面由 Unity 三维模型实时渲染。

原始提示词：

> Create an original premium indie game key art for a Chinese 2.5D rogue-lite beat-em-up game titled conceptually Mist Lantern Citadel, no text in the image. Landscape 16:9 composition. The LEFT third is atmospheric dark indigo negative space reserved for menu typography. On the RIGHT two thirds a striking small stylized 3D hunter with an oversized weathered teal hood, glowing warm amber lantern held in one hand, curved silver saber in the other, terracotta red scarf, leather belts and brass buckles, short chunky proportions but carefully sculpted hands and boots, face in shadow with two small warm eyes. He stands on wet cobblestones on an old gothic bridge looking toward a huge crooked clocktower emerging from turquoise fog in the distance. Several charming sinister little porcelain mask goblins with teal glowing eyes emerge from mist further away, never gore. Hero silhouette highly readable. Sculpted toy-like 3D forms with painterly textured surfaces, bevelled stone edges, richly modeled architecture, cinematic rim lighting, deep indigo / smoky petrol blue / patinated brass / amber glow / muted coral accent. Layers of stone archways, lantern posts and brass clockwork, delicate floating fireflies. Polished art direction, cohesive shapes, ambitious student game portfolio quality. Avoid photorealism, avoid anime, avoid copying any existing game characters, avoid generic geometric placeholder models. No words, no logos, no watermark, no UI.

## 字体与声音

中文使用 Noto Sans SC 的衍生静态字体，SIL Open Font License 位于 `Assets/Resources/Fonts/OFL.txt`；随项目附带字体来源说明。
TextMesh Pro 的 Liberation Sans 及着色器随 Unity 包提供，相关许可保留在 `Assets/TextMesh Pro`。
环境音与音效由 `Presentation.cs` 中的 Sound 类合成，无第三方音乐。默认关闭声音，失焦暂停环境音；测试参数 `-silent-test` 或 `-qa` 强制输出为零。
