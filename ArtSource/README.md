# 雾灯遗城实体模型源文件

`Models` 包含 3 种玩家武器组合与 7 种敌人的 OBJ/MTL，可导入 Blender、Maya 等三维软件检查与编辑顶点。每个建模分件是独立 object，名称保留其 Unity 变换层级。模型具有真实厚度与背面结构；没有角色图片面片。

- `Hunter0`：弯刀守灯人；`Hunter1`：钟锤守灯人；`Hunter2`：灯杖守灯人。
- `Enemy0`：瓷面游魂；`Enemy1`：持盾铜偶；`Enemy2`：夜蛾术士；`Enemy3`：雾犬。
- `Enemy4`：守桥铜卫；`Enemy5`：荆冠园主；`Enemy6`：第十三声钟。

OBJ 使用右手坐标、Y 向上，单位与 Unity 一致。导出为原始分件的静止姿态；OBJ 不包含骨骼动画。运行时的前臂、胫部、翼与披布动画以及顶点遮蔽、卡通着色和材质图集仍在 Unity 预制体与 Shader 中。MTL 保存基础色，不把标准材质软件里的预览误称为与 Unity 实机完全一致。

生成入口：`Mist.Editor.ModelExport.Run`。几何定义：`Assets/Scripts/Sculpt.cs`。Unity 中可直接检查 `Assets/Scenes/ModelGallery.unity` 和 `Assets/Resources/Models`。
