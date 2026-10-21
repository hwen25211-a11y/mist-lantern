# 实模重做与《失落城堡 2》质感对照

本记录针对用户对 v1.0“像贴图、像 toy”的反馈。v1.0 的流程测试不能证明美术达到要求；原有发布标签保留为历史版本。本分支进行几何、材质、关节和场景重做，不能把“生成成功”或三角形数量当作视觉验收。

## 对照依据

原作对照使用 [Hunter Studio 的 Steam 商店](https://store.steampowered.com/app/2445690/Lost_Castle_2/) 公开实机图片。原作介绍为 2D 横版动作游戏；比较的是最终画面中的轮廓、形体、装备结构、明暗、动作与场景层次，不把原作的画法误称为三维模型。

- [普通战斗与人物 V05](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2445690/ss_aa08e8ae7cea774a66183d9cc1ec9afddcae313d.1920x1080.jpg)
- [大型首领 V06](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2445690/ss_8d1df40636f22a4efd402a43632d9d7b1fb4f754.1920x1080.jpg)
- [环境 V02](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2445690/ss_17be0a623d5f404bde03d55d31e299de7f122d1d.1920x1080.jpg)

原作图仅作对照引用，不放入游戏、模型或发行包。

## 本轮结构

- 人物采用独立三维厚壳、截面扫掠、曲面、倒角面片和曲线构件。帽兜有开口及内衬，肩甲有叠层，衣摆及披布有厚度和折面。
- 皮带、扣具、指节、靴筒、鞋底、武器刃面、钟口、晶体和星盘均为几何结构。模型保留分件及分关节合并后的绘制网格。
- 三位首领有不同躯体：负钟卫士、盘根树体与瓷面、带机械臂和摆锤的钟楼构装体。普通敌人也保留不同轮廓。
- 不同表面使用布、皮革、铁、铜、瓷、木、石和发光材质。模型凹隙遮蔽烘入顶点；不依赖一张角色图片面片。
- 肘部、膝部、翼、披布与提灯独立活动。武器随持握前臂运动。
- 近、中景为几何地砖、石拱、石柱、栏杆、木桶、树木、管道、齿轮及碎石。遥远景物仍使用绘景；石材使用色彩纹理，不能把“真实几何”误写成“完全没有纹理”。

## 迭代审查

| 轮次 | 发现的问题 | 判断与处理 |
| --- | --- | --- |
| v1.0 基线 | 大首领接近放大小兵；布、石、金属响应相近；几何简化；陈列底座丢材质 | 不达标，保留 before 图 |
| 第 1 轮 | 有实体分层，但圆形脸颊像玩偶；部分衣摆法线反向；复杂背景与简陋近景割裂 | 不达标，重塑脸部、修正衣摆，重做近景 |
| 第 2 轮 | 新建筑、树与地面之间有空隙；铺路过度规则；部分凹轮廓扇形三角化错误 | 不达标，调整地基、碎石、材质；使用凹多边形三角化 |
| 第 3 轮 | 过宽的实体地面吞没远景层次；角色与杂兵身份差异仍弱 | 不达标，恢复低对比远景与实模近景的分工，添加首领独有构件 |
| 第 4–5 轮 | 镜像翼片朝向错误；背面头部穿插；关节仍偏僵硬 | 不达标，修正负行列式变换后的三角面顺序，分离前臂和胫部 |
| 第 6–11 轮 | 曲面尖角的平均法线翻转、发布包遗漏旧特效 Shader | 尖角拆分法线；补齐发布 Shader 依赖；实际重玩原生版完整通关 |
| 第 12–14 轮 | 用户指出人脸难看、眯眼，不接受面具与凶相造型 | 改眼睛和圆润脸型仍然简陋，用户明确拒绝幼态、大眼、玩具感；这些模型不通过美术验收 |
| 人物重建 | 基础体块拼装无法提供完整眼眶、鼻翼、口周和耳廓结构 | 停止在旧脸上添加零件，转向完整人体拓扑和 Blender 服装制作；先检验成年主角样板。底模与许可证见 `ArtSource/Characters/README.md` |
| 人体拓扑样板 | 用户指出过于像真实人类，偏离《失落城堡 2》的卡通造型 | 美术不通过；完整人体拓扑与绑定管线仅作为技术资产，不能作为定稿人物交付 |
| 卡通变形样板 | 比例压缩后依然有长颈、写实眼眶、叶片状发束、偏薄的靴子；衣甲轮廓与设计稿差距明显 | 美术不通过；停止把局部尺寸调整等同于还原 |
| 已确认设计稿 | 用户明确认可 `ArtSource/Characters/Concept/keeper-cartoon-v1.png` 并要求 1:1 还原 | 将该图固定为验收依据；正背 A 姿参考是辅助建模图，不能冒充模型实拍；正在制作实际三维网格 |

2026-10-21：补齐了骨骼蒙皮身体的通用几何检查、静态 OBJ 导出和离线姿态截图路径。
第一次扩大检查范围后，旧人工模型的靴子钢片被检出局部绕序/法线不一致，仍属于未通过候选。
当前 `WARDEN_CHECK_OK` 只证明既有权重与有限姿态运算成立，不能替代造型、完整几何或新版本流程验收。

## 每轮必须看的证据

1. `docs/screenshots/sculpt-review/game-area-0.png` 至 `game-area-2.png`：同一游戏镜头下的三个区域与首领。
2. `Hunter0/Enemy0...Enemy6-view-0/1/2.png`：前斜、侧面、后斜。镜头必须容纳整个模型，不允许隐藏背面问题。
3. `*-clay.png`：去除彩色表面，检查实际形体、厚度与遮挡。
4. 攻击、行走、格挡、放灯时的发布版截图与测试记录：静态图不能替代动作验收。
5. 与上面的原作实机在相近画幅和角色像素高度下比较。角色辨识、材质分区、剪影、动作与画面统一度都要检查；不能只比较细节数量。

测试与构建日志在 `docs/qa/sculpt-review/`，几何源码在 `Assets/Scripts/Sculpt.cs` 和 `SculptEnvironment.cs`，实际游戏着色器为 `Assets/Shaders/Sculpt.shader`。

## Approved Keeper: local reconstruction and integration (2026-10-21)

The authenticated Pixal3D preview completed, but mesh extraction requested 240 GPU seconds and exceeded the service limit. No cloud GLB was obtained. The user chose local production without purchasing service.

A pinned Hunyuan3D-Swift MLX port produced the actual 484,441-vertex / 968,862-triangle watertight source on M1 Pro. It was normalized, reduced to 180,000 body triangles, given four orthographic geometry-aligned paint guides, and baked to a 4096px atlas. The copper shoulder shell was separated and thickened, with a flexible dark under-sleeve; held equipment and rear gear remain separate solid geometry.

Rejected iterations and fixes: front/back-only projection produced ghosted side details; selected-to-active cage baking sampled neighboring arms; image-color-based weights created severe arm/cape spikes; bone heat leaked into the opposite arm and distant leg; the first rigid shell averaged opposing normals at its thin rim. These were replaced with four-view visibility-aware projection, same-object emission baking, anatomical weight restrictions, length-weighted surface smoothing, rigid armor ownership and hard rim normals. Hidden palm/cape colors and equipment positions were then corrected.

Actual Unity evidence: `docs/screenshots/character-rebuild/unity-hero-0.png`, `unity-hero-1.png`, `unity-hero-2.png`, `unity-face.png`, `unity-deformation.png`, and `unity-clay.png`. The clay render exposes the actual geometry, while painted facial and surface details remain visibly distinct from the underlying sculpt. Concept sheets are never reported as in-engine output.

Compare against the approved design on proportions, brown swept hair mass, teal hood, right copper pauldron, layered charcoal/leather costume, normal cartoon eyes, cleaver and raised lantern. Public Lost Castle 2 imagery remains a style reference only; its original 2D assets are not copied into the Unity project. Current gameplay/build acceptance is recorded separately in `docs/QA.md`.
