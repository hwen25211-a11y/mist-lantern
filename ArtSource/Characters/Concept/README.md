# Approved keeper design

`keeper-cartoon-v1.png` was generated with the built-in imagegen tool in reference
generation mode on 2026-10-21. The user explicitly approved this exact image and
requested a 1:1 model reconstruction. It is a concept sheet, **not a screenshot of
the Unity model**. Runtime meshes must be compared against it before delivery.

Reference roles: the two official Lost Castle 2 screenshots were style references
only. They are not included in the game or release. The original keeper design uses
dark teal cloth, copper armor, a lantern clasp, gloves, short boots and a cleaver.

Auxiliary modeling references (both generated with the built-in imagegen editor):

- `keeper-model-front-v1.png`: derived from the approved sheet, transparent,
  orthographic front A-pose with loosely closed gloves, no handheld equipment.
  The brief explicitly preserved the same adult cartoon face, hairstyle,
  right copper pauldron, outfit and palette. This is an aid to reconstruction;
  the approved sheet remains authoritative where proportions differ.
- `keeper-model-back-v1.png`: derived from the front reference and approved sheet,
  transparent orthographic rear A-pose. Its generation prompt is recorded below.

Rear-reference prompt:

> Use case: character reconstruction technical reference. Create ONE strict orthographic REAR VIEW of the EXACT SAME approved cartoon lantern keeper in image 1. Image 2 is the approved design sheet and provides his rear costume design. This is not a redesign or a new character. Preserve exactly the compact 3-head adult cartoon proportion, wide angular head, huge dark brown solid swept spiky hair mass, broad shoulders, short sturdy torso and limbs, thick gloves, stout short boots. MATCH image 1's front A-pose: arms held diagonally down about25 degrees away from torso, elbows only slightly bent, loose closed gloved fists, feet apart, level camera no perspective. No handheld weapons or lantern. Exact back design from image 2: dark teal thick rolled hood pushed back behind neck with broad triangle hanging over shoulders, torn dark teal short mantle extending to knees in asymmetric pointed tails, brown diagonal leather bandolier across back with small square brass buckle, brown belt and two compact tool pouches/rolls at lower back, copper layered pauldron on anatomical RIGHT shoulder (viewer right in rear view), brown bracers, chunky gloves, dark charcoal pants, broad strap leather boots with solid metal heel edges. Hair back follows SAME swept clumps as image1, no bald patches, no realistic individual strands. Same scale, full body bounds and vertical alignment as image1: top hair near y30px, bottom boot near y1215px in square1254 canvas. Camera centered at front-image character center. Render crisp broad faceted hand-painted cartoon material shading exactly matching the reference with dark outlines, subtle restrained weathering. Transparent background. One rear full body figure only. No floor or cast shadow, no text, no logos, no sheet layout. This is a reference for building an actual3D mesh and must clearly preserve the approved silhouette and all outfit elements.

The approved design sheet's exact generation prompt follows:

> Use case: stylized-concept. Create a finished original character MODEL DESIGN SHEET for a Unity fantasy beat-em-up. The two input images are STYLE REFERENCES ONLY: study the compact cartoon adventurers in the Lost Castle 2 gameplay shots, their broad graphic shapes, strong outlines, exaggerated proportions, clear armor layers and restrained face detail. Do not copy a specific reference character, logo, environment or screenshot. Original subject: a male lantern-keeper adventurer, weathered but lively adult attitude, a short dark teal hooded travel mantle pushed BACK to expose his face, copper layered shoulder plate, fitted charcoal tunic, one leather bandolier with square buckle, heavy short boots and gloves, a warm brass lantern in left hand and a curved steel cleaver in right. Physical design: genuinely compact fantasy cartoon proportions, about 2.7 heads tall including hair, wide angular head, short sturdy torso, short substantial limbs, boots and gloved hands with weight. Face: stylized planes and confident expressive eyebrows, ordinary clearly open almond-shaped eyes with small dark pupils, understated angular nose and small expressive mouth, slight asymmetry; NOT a real human face, NOT a toddler face, NOT huge anime eyes, NOT a baby doll. Hair: 6 to 9 solid chunky swept dark-brown locks, distinct tapered volumes, no thin individual realistic strands or transparency cards. A copper lantern-shaped clasp is the identity motif. Show the SAME character in three coherent full-body views, front three-quarter, strict side, and rear, with one face detail inset. Large rendered game-ready stylized 3D form language, dark crisp outlines, two to three broad value bands, hand-painted-like restrained material color, visible solid armor bevels and sewn cloth hems. Clearly constructed forms and believable gear assembly; no floating straps or disconnected props. Deep teal, aged copper, charcoal, warm parchment skin, amber lantern. Neutral warm gray studio backdrop, clean organized sheet, no labels, no text, no logos, no watermark. This is a target design sheet, not an in-game screenshot. Avoid tall realistic human anatomy, hyperreal skin pores, glossy plastic, elementary primitive assembly, small noisy rivets, fluffy round baby cheeks.

## Geometry-aligned paint guides

`keeper-paint-front-v1.png`, `keeper-paint-back-v1.png`, `keeper-paint-left-v1.png` and `keeper-paint-right-v1.png` were generated with the built-in image editor using actual orthographic clay renders and the approved sheet. Camera, silhouette and feature locations were constrained. These guides supply painted colors on real geometry; they are not model screenshots. The rear guide deliberately omits a flat-painted pack because the game supplies separate solid equipment. View-specific briefs and image hashes are in `paint-provenance.json`.
