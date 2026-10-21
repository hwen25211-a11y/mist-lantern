# v1.0 原创绘画资产

遗物图集经过一次边界修订，运行时按独立区域的 UV 边界取图并保持图案比例。最终修订提示词：

Edit this existing inventory atlas. Preserve the exact twelve relic identities and their painted style, but RE-LAYOUT precisely into a square transparent 4-column by 3-row grid. Each cell has exactly equal dimensions. Scale every complete relic so it fits entirely within the CENTRAL 65 PERCENT of its own cell width AND height, leaving at least 17.5 percent fully transparent padding on all four sides inside EVERY cell. No part of an object, ribbon, flame or shadow may cross a cell boundary. Row 1: wick, bandage, jade lampshade, mirror. Row 2: winged boots, porcelain talisman, scroll, oil goblet. Row 3: ember, potion bottle, bell, scarf. Precisely uniform row heights and column widths, no borders, no background, no text. Leave generous empty transparent space around each object. Do not add or delete any objects.

生成工具：内置 imagegen。原作截图与视频仅作研究，未作为游戏素材打包。

模型、场景部件与武器由本项目 Art.cs、Models.cs 制作并由 Unity 导出；环境绘画和遗物图标为 AI 辅助生成的原创位图。模型可编辑，绘画在运行时通过纹理坐标选择图集区域。

## Worlds-v2.png

项目位置：Assets/Resources/Art/Worlds-v2.png

最终生成提示词：

Use case: stylized-concept. Asset type: original background atlas for a Unity 2.5D dark fairy-tale action roguelite named Mist Lantern. Create ONE square 2048 x 2048 image divided into THREE equally tall horizontal panoramic panels, edge to edge, no gaps or borders, no text. Each panorama is an original hand-painted cartoon fantasy game environment, confident ink outlines, rich layered illustrated depth, restrained high quality brushwork and material detail, cool shadows and warm lantern light. Camera faces the environment from a slightly elevated side view, NOT isometric. Each panel must have a broad quiet lower third empty ground area; all architecture in back half. Top panel: abandoned old stone bridge crossing a mist-filled canal city, crooked gothic rooftops and arches, hanging amber lanterns, aged blue-gray stone, copper trim, distant towers fading in teal fog. Middle panel: haunted walled botanical garden with twisting ancient trees, broken greenhouse arches, lush dark teal leaves, pale porcelain fragments, small golden blossoms, mossy stone and distant fog, forest green and jade palette. Bottom panel: interior of an immense ruined clock tower, giant bronze clock face with carved marks not legible numbers, layered gears, hanging chains and deep violet shadows, warm amber light through shattered windows. Atmospheric but readable, moderately bright midtones, clearly shaped silhouettes, beautifully authored indie action game backgrounds. No people, no characters, no monsters, no UI, no lettering, no logos, no vignette. Three full width horizontal scenes of exactly equal height, stacked top middle bottom; consistent visual style. These are original game assets, not copies of any existing game.

## Ground-v2.png

项目位置：Assets/Resources/Art/Ground-v2.png

最终生成提示词：

Use case: stylized-concept. Asset type: original hand painted ground material atlas for a fantasy action game. A wide 3072x1024 image with EXACTLY THREE equal square texture panels arranged side by side, no border or margin. Pure orthographic top-down material views, no perspective, no objects standing up, no text. Each panel is independently seamless tileable and fills its square. LEFT: ancient dark blue-gray bridge paving, varied irregular broken flagstones fitted closely, very narrow cracks, subtle worn carvings and occasional moss in seams, softly painted rough edges, not uniform grid. MIDDLE: deep muted green gray garden ground, mostly packed earth and small worn stone fragments, roots and soft moss patches with irregular organic edges, no large foliage. RIGHT: ruined clock tower floor, desaturated violet slate slabs with fine narrow antique bronze inlaid lines and occasional broken mosaic shapes, softly worn. Style rich hand painted stylized game materials, moderately low contrast, dark enough for colorful characters to stand out, elegant subtle texture, no hard baked directional shadows, no highlights suggesting wet plastic, no realistic photography. Large broad quiet surfaces with restrained micro texture, consistent scale in all three panels.

## Relics-v3.png

项目位置：Assets/Resources/Art/Relics-v3.png

最终生成提示词：

Use case: stylized-concept. Asset type: inventory icon atlas for an original dark fairy tale game Mist Lantern. One transparent PNG, square 1536x1536, precise regular 4 columns by 3 rows, TWELVE equally sized icon cells, each object centered with plenty of empty transparent margin, no borders and no text. Hand painted illustrated collectible relics with confident dark ink contours, warm copper and muted jade palette, dimensional shapes, crisp at small sizes, sophisticated indie game art. Row 1 left to right: 1 a bright burning braided lamp wick in copper holder, 2 a rolled ivory bandage with red wax seal, 3 a carved jade lantern glass shade, 4 a small ornate antique copper hand mirror. Row 2 left to right: 5 leather boots with small pale feather wings, 6 a cracked porcelain talisman on red cord, 7 a rolled dark parchment contract tied with jade ribbon, 8 a copper goblet full of golden lamp oil. Row 3 left to right: 9 a black ember stone with vivid orange cracks, 10 a squat green potion flask with copper stopper, 11 a bronze bell engraved with concentric arcs, 12 a long flowing red scarf with gold embroidered hem. No characters, no lettering, no UI, no background, no duplicates. All twelve objects fully visible, isolated, each confined to its exact grid cell.
