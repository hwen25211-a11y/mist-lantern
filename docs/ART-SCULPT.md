# 实体模型与材质来源

主角以用户确认的 `keeper-cartoon-v1.png` 为目标，采用本地 Hunyuan MLX 三维重建，四个与实际网格对齐的绘制视图烘焙为 4096 像素图集，随后拆出实体铜肩甲、绑定 17 个控制骨骼。可编辑源文件为 `ArtSource/Characters/Keeper.blend`、`Keeper-structured.blend`、`Keeper-rigged.blend`；完整来源、许可和制作入口见 `ArtSource/Characters/README.md`。主角不是从零手工雕刻，旧 MakeHuman 和毛发实验已从当前交付素材中移出。

提灯、弯刀与背部装备在 `SculptKeeperWeapon.cs` 中定义，敌人在 `Sculpt.cs` 中定义，环境在 `SculptEnvironment.cs` 中定义，均输出真实三维网格。此前圆脸、大眼、面具和写实主角没有通过美术要求，它们的历史构建与玩法测试不作为当前角色的验收依据。

角色保留原始分件以及按活动关节合并的渲染网格。Unity 编辑器中的 `Assets/Scenes/ModelGallery.unity` 可从任意角度检查；原始分件的 Renderer 默认关闭，以免与合并网格重叠。需要编辑分件时，关闭相应 `Sculpt render` 并启用原始分件显示。

当前主角的三维体积包括成束头发、脸部、厚帽兜、肩甲、分节护腕、手套、衣摆、靴子，以及独立的提灯、刀刃、卷毯与皮包。眉眼、扣件和磨损的细绘信息来自烘焙图集。敌人保留厚面具、警钟、树枝指、内部齿轮与摆锤等独立构件；它们与主角使用同一游戏光照和轮廓呈现。

凹隙遮蔽由编辑器对实际网格做短距离射线采样，写入顶点 Alpha；Shader 把它当作遮蔽量，表面保持不透明。三角面朝向、镜像变换、有限顶点数据和资源引用由 `Assets/Editor/ModelChecks.cs` 验证。视角与无彩色材质截图由 `SculptReview.cs` 生成。

## 新增材质图集

- 文件：`Assets/Resources/Art/CraftSurfaces-v1.png`。
- 生成方式：内置 `image_gen` 工具，经 imagegen 技能流程生成；未使用 CLI/API fallback。
- 内容：四格灰度材质——布、皮革、旧铜、钢。图像只提供表面色纹，由 Shader 着色；不会替代人物几何。
- 使用：关节局部坐标三向投射，纹理随角色关节移动；四格分别采样，留出边缘间隔避免串格。石材继续使用原有地表图集。
- 默认生成原文件保留在 `/Users/Admin/.codex/generated_images/01a15957-fa1e-74e1-8ea8-cedf5e4592ef/exec-0c62a164-042f-4118-87ae-70c9fe751e35.png`；项目使用上面的独立副本。

完整生成提示词：

> Use case: stylized-concept. Asset type: a production albedo material atlas for fully modeled 3D characters in a dark fantasy action game. Create one square 2048x2048 image, divided into exactly four equal edge-to-edge square material swatches, a precise 2 by 2 grid, NO gutters, NO borders, NO labels. Top left: hand-woven heavy linen cloth with small visible woven threads, subtle irregular dyed patches and painterly broad grain; top right: worn supple leather with fine pebbled grain, gentle creases and irregular rubbed patches; bottom left: hand-hammered old bronze surface with restrained dark patina, broad hand-painted scuffs and tiny hammer marks, no objects or engravings; bottom right: forged steel with subtle directional brushing, broad worn patches and fine scratched grain. ALL FOUR SWATCHES MUST BE NEUTRAL GRAYSCALE, average middle gray, with moderate local value range and no colored tint, so the game shader can tint each material. Each swatch must be uniform material with variations spread evenly, tileable at its own four edges, flat evenly illuminated orthographic material scan, no directional cast shadows, no sphere, no rendered specimen, no perspective, no cloth folds or draped objects, no seams dividing a swatch. Professional stylized hand-painted game material, carefully designed broad-to-fine surface detail. Do not draw armor, a person, an item, a UI, a background scene, text or watermark.

远景绘景、地表及遗物图标的原始来源和生成说明仍见 `ART-V1.md`。《失落城堡 2》公开图片只作为研究对照，不包含在游戏资产中。
