# GHA separation baseline — September 23, 2026

**Historical baseline, not current implementation status.** Subsequent Home lifecycle extraction
and the October 1 scoped-index experiment are recorded in the
[current handoff](GHA-SESSION-HANDOFF-2026-08-25.md) and controlling runbook. Further E3 performance
work is owner-deferred until after P5-4 (UMA 3.1). The statements below describe the September 23
baseline; they do not mean that no runtime changes exist today.

Status: Phase 0 plan content approved; inventory and bounded Desktop Home checks recorded.
Phase 0 execution is **not complete**: PC Link evidence and documentation/inventory review remain
open, and the proposed local documentation commit has not been approved or created.
BUG-3 walking investigation is deferred by the owner. No runtime
implementation or asset refactoring was performed.

The September 23 implementation plan controls sequence; the migration runbook controls acceptance.
The owner explicitly approved entering/exiting Play Mode for Desktop baseline checks in this task.
The owner also directed that nothing be pushed: any approved commits must remain local.

## Revisions and environment

- GHA HEAD and local `origin/Generic-Humanoid-Avatars`:
  `8c19b5f773985b71045df92337fc107595512d56`.
- Official upstream `Master`: `58bf4e775653c8befbc6d36584742360dbc7033a` (1.1.9),
  verified with a read-only remote-reference query during this pass. It matches local
  `origin/Master`; no fetch, merge, branch change or upgrade was performed.
- Index empty at the start of the pass. The September 18 fixes are already in HEAD.
- Unity `6000.3.11f1`, Windows Standalone target, VertexForm Desktop platform.
- Pipeline is actually `0.6.0-exp.1` in the manifest and lock; older 0.5.0 evidence is historical.
  AnkleBreaker remains pinned at `4c03a9601a8c5133bfca1b580b23866a96b0b603`; both embedded
  GHA records remain intact. `com.unity.collab-proxy` is absent by owner direction.
- Recorded UMA source candidate remains `c9204fe4` with its documented destination-only shader
  overlay. This pass did not resync or re-certify vendor contents, or validate Store/release packages.
- All four official VertexForm Notion links in AGENTS.md were attempted and remained inaccessible.
  Source findings below do not claim independent verification of those pages.

The supported CLI found the exact running GHA editor after permitting access to its protected
connection descriptor. No token was displayed. This CLI rejected `pipeline list --project-path`;
package state was instead established from manifest/lock plus the working project-scoped
`status`/`command` interface. No tooling install or fallback editor connection was used.

## Existing working-tree ownership

The pre-test inventory contains 101 tracked/untracked entries:

| Classification | Entries | Treatment |
| --- | ---: | --- |
| Planning documentation | 10 | Existing September 23 edits; retain and propose separately for a local checkpoint |
| Owner package edits | 2 | Preserve collab-proxy removal and manifest ordering; exclude from documentation checkpoint |
| Import metadata | 61 | Added Android/WebGL texture-import defaults and metadata; preserve, exclude |
| Owner/project settings | 12 | Platform, XR, build profile, quality, renderer and editor settings; preserve, exclude |
| Sensitive local configuration | 2 | Photon settings and local agent configuration; values neither read for this inventory nor copied |
| Generated state | 3 | UMA project metadata and solution state; exclude |
| Unreviewed local tools | 3 | Existing icon/swatch/foreground scripts; retain, exclude from this checkpoint |
| Desktop-prefab serialization | 1 | Four null non-WebGL MR fields missing; preserve, exclude |
| Other local work | 7 | Ignore-rule addition, owner instructions/content and obsolete metadata deletion; preserve, exclude |

Specific reviewed deltas:

- Package delta removes only `com.unity.collab-proxy` 2.11.4 from manifest/lock; the Pipeline
  manifest entry was reordered, not upgraded by these unstaged edits.
- The Desktop player prefab removes only the four null `MixedRealityHandler` fields documented
  in the runbook. No prefab was saved in this pass.
- Platform choice is Desktop; Android Touch Plus profile is enabled; project identity is owner
  configuration. A Meta Quest build profile/quality entry is present. These are not host code.
- Texture metadata adds platform-import records; renderer assets contain shader-prefilter changes.
- The font asset, URP global settings and GraphicsSettings appear in status but have no normalized
  Git content diff. Do not manufacture a cleanup commit for them.
- The `.utmp/` ignore addition and deleted exporter metadata are independent of this slice.

The exact paths appear in Appendix A. SHA-256 values for 98 existing nonsensitive files are retained
locally in `Logs/SeparationBaseline-20260923/working-tree-before.csv`; all 98 were unchanged after
the runtime checks, before this report/runbook update. Sensitive files were deliberately not hashed
or copied. The deleted file has no current hash.

## Committed delta and ownership dependencies

There are 232 changed paths between verified upstream and GHA HEAD. Appendix B classifies every
path by intended destination: 128 host, 75 UMA integration and 29 mixed/review. This is an ownership
inventory, **not an accepted move/export manifest**. Mixed serialized assets and bootstrap code
need extraction; animation redistribution rights and exact export membership remain Phase 2 work.
No path is approved for deletion by appearing here.

Source inspection reconfirmed:

1. Classic Save persists mode 0, invokes `AvatarProviderSelection.RequestApplySavedAvatar`, and
   displays Saved without checking its return. The only listener found is
   `UmaHomeAvatar.Refresh`, behind both host and UMA defines. A host-only Save therefore lacks
   the current Home apply owner.
2. `UmaHomeAvatar` also owns legacy suppression, Home startup, scene teardown, Classic body
   construction and presentation. These shared responsibilities must leave UMA first.
3. `UmaAvatarBridge.RegisterHook` owns `PlayerNetworkSetup.AvatarConstructionOverride` and
   constructs UMA directly. `ApplyLocalStockAvatar` and `ApplyLocalRecipe` have declarations but
   no callers in the inspected GHA runtime; Home Save evidence does not prove in-world propagation.
4. The existing `AvatarConfigurationProviderRegistry` registers UI panels. No runtime registry
   maps stable mode IDs to construction/disposal providers.
5. `AvatarExtensionSync` is already opaque Fusion transport. Its `WriteData` currently lacks its
   own authority guard and `ReadData` trusts network `Length`; Phase 1 must verify authority and
   length handling without silently altering the baked layout. No network test was run here.
6. `GhaIntegrationBootstrap` owns both host and UMA menus and detects UMA by one source-file
   path. Shared panel/Home/player assets still carry provider wiring. File removal is premature.

Core-delta review also confirms the independent Addressables local-bundle condition fix, Web
serialization rename preservation, and SceneLoader resolution timeout/EventSystem changes. These
remain separately reviewable; the old transitional host patch is not a complete representation of
all committed core differences.

## Desktop evidence

The checks invoked existing runtime Button events/Slider events through supported CLI one-shot
scripts outside Assets. They do not establish mouse-raycast or headset-input acceptance. No runtime
validation component was installed. Original avatar and login preferences were saved privately in
Editor SessionState and restored afterward; values were not included in the checkpoint.

XR startup was temporarily disabled in memory for each session. `XRSettings.isDeviceActive` was
false at login. No settings asset was saved. All three sessions began from LoginScene using
the ordinary anonymous-login action.

| Case | Observed result | Evidence under `Logs/SeparationBaseline-20260923/` |
| --- | --- | --- |
| Initial Home load | One ready saved UMA puppet, no new errors | Structured CLI readback |
| Select Classic before Save | Saved mode remains 1; legacy body stays inactive; UMA preview is hidden | `classic-before-save.json` |
| Classic Next | Stock selection 0 to 1; saved UMA body remains active until Save | `classic-next-before-save.json` |
| Classic Save | Mode 0; legacy body active; UMA puppet/root removed | `classic-after-save.json` |
| Female preview | Female preview builds while saved Classic body remains | `female-preview.json` |
| Female height and Save | Slider 0.65 encodes 166/255; Home DNA reads 0.6509804; legacy body hidden | `female-after-save.json` |
| Repeated Save rebuild | Ready puppet; both new renderers remain on first-person hidden layer 7 | `female-rebuild.json` |
| Female to Male | Male preview builds while Female remains the saved body until Save | `male-return-preview.json` |
| Save/close/reopen | Home and settled preview both have saved Male height 0.6509804 | `male-reopened-settled.json` |
| UMA restart | Saved Male/height reconstruct; one player DCA; legacy body hidden; renderer layer 7 | `uma-after-restart.json` |
| Switch back to Classic | One stock body, UMA torn down; screenshot reviewed | `classic-saved-second-session.json`, `studio-classic-saved.png` |
| Classic restart | Mode 0, index 1, legacy active, no DCA or UMA puppet root | `classic-after-restart.json` |

The first reopen snapshot caught preview DNA at 0.5 before asynchronous build completion. The
settled snapshot confirmed 0.6509804; do not treat intermediate build state as a persistence failure.
`studio-male-saved.png` was also visually reviewed: Studio preview and Home mirror were present.

Console errors: zero captured between cursor 258 and 493, with no dropped results. Compilation
was completed without errors before testing; the final recompile request reported `up_to_date`
(no scripts needed recompilation). This is not a fresh player build or clean import.

Restoration verified original existence and value for the three mode/stock integer keys and four
recipe/legacy/login string keys. XR startup restored to true. Editor returned to stopped LoginScene,
scene clean. No runtime source, scene, prefab, package or project settings edits were made.

Owner decision after this baseline: defer BUG-3 walking investigation until later. The owner has
seen the issue and received another person's report, but cannot reliably reproduce it. Retain it
as unresolved work; capture evidence if it recurs. This deferral does not close general locomotion
acceptance or defer PC Link.

Still open: PC Link/BUG-10, broader BUG-4 reproduction, color/outfit
regression, stock-only with UMA physically absent, non-UMA proof provider, two clients/authority/
late join, world transitions/seating, standalone/device/platform and acquisition/distribution gates.
These results extend Desktop evidence for BUG-1/2/11 only; they do not close those broader gates.

## September 24 PC Link observations — partial A5 evidence

The owner connected the headset and entered Play Mode manually. During the latest VR Home
instance, the owner saw the UMA avatar in the mirror before it appeared when looking down.
Captured Console evidence shows the body/arm IK ready around 20:27:38 UTC, while the first-person
body stayed hidden until height calibration completed at 20:27:50 UTC (about 11.9 seconds later).
Floor readiness was available earlier; scale calibration was the remaining visibility condition.
The calibration code waits for a stable standing-height window. Reported height varied and a
focus-triggered tracking reinitialization occurred; the logs do not establish the source of that
variation. The delay was after avatar construction, not another 12 seconds of mesh generation.

The owner subsequently confirmed the body was properly visible and reported no other issues in
that observation apart from these two, explicitly requested as later fixes:

| Plan ID | Owner observation | Disposition |
| --- | --- | --- |
| A5.1 | Arms go inside the body | Deferred for later arm/torso fit and IK assessment; not fixed |
| A5.2 | Viewpoint feels too far inside/back on the body; looking down shows too much upper chest | Deferred for later eye/body alignment assessment, with the desired view farther forward toward the feet; not fixed |

The earlier plan had general embodiment/calibration work but no explicit rows for these two
observations. They are now tracked separately under A5. Do not assume A5.2 requires moving the
tracked camera itself; determine the appropriate avatar/eye alignment correction during that work.
The initial visibility delay remains an A5 finding to review, without a fix or blanket acceptance.

Ignored local evidence: `Logs/SeparationBaseline-20260923/pc-link-20260924-visibility-console.json`
and `pc-link-20260924-visibility-finding.md` in the same directory. Latest relevant Console
sequences: 647–717; capture ends at cursor 764. No exception appeared in that interval; pre-build
arm-rig warnings recovered once the skeleton was created. The captured file also contains earlier
Desktop/VR attempts, which must not be conflated with the latest Home instance.

At the September 24 observation, A5 remained partial: Save/body-type/rebuild checks, controlled tracking recovery, Desktop-return
arm behavior and final restoration still require explicit results. The owner's general report
that everything else seemed okay does not establish those unreported cases. No runtime fix,
scene/prefab change, camera adjustment or agent-controlled Play Mode transition was made during
this investigation. BUG-3 walking remains deferred separately.

## September 26 owner verification decision — reuse prior results

The owner confirmed that avatar changes and saving had already been tested successfully when
those changes were implemented, declined repeating those checks, and instructed proceeding.
Accept that prior owner verification as baseline evidence; withdraw the request to repeat editing
and Save merely because this plan is beginning. No new Save/rebuild test was run on September 26.
Use the existing recorded Desktop checks and September 24 headset observations alongside that
confirmation. Retest relevant behavior after code changes or a specific newly observed failure.

The two comfort issues A5.1/A5.2 remain deferred, and the initial calibration/visibility delay
remains recorded for review. Distinct unreported cases, including controlled tracking recovery,
VR-to-Desktop arm handoff and final settings restoration, must not be marked passed by this
confirmation. Do not conflate their status with the withdrawn duplicate edit/Save request.
Documentation/inventory review and separate local-commit approval remain outstanding; nothing
has been committed or pushed. The plan's next-phase review requirement remains in effect.

## Phase 0 completion checklist after owner plan review

The owner approved the content of Phases 0–3 and Phase 5, conditionally accepted Phase 4, and
requested a fresh review of each next phase after its predecessor completes. This does not mark
runtime evidence accepted or authorize the documentation commit. No Phase 6 belongs to the current
plan; older runbook phase numbering is historical.

| ID | Prepared/completed evidence | Remaining to close |
| --- | --- | --- |
| P0-1 | Exact working-tree and committed-delta inventories, with exclusions, in this report | Owner reviews documentation/inventory scope |
| P0-2 | Official 1.1.9 comparison revision verified | Complete for this recorded baseline; no upgrade implied |
| P0-3 | Current Editor compilation and bounded Desktop Home checks recorded | Complete for that scope; clean import/build and broader tests remain later gates |
| A5 | September 24 Home observations/diagnostics; September 26 owner confirms prior edit/Save testing | No duplicate edit/Save run required. Distinct tracking-recovery, Desktop-return and restoration results remain unreported, not assumed passed. |
| C2 | Exactly 11 documentation files staged for review | Owner review and separate approval before creating the local commit |

Read-only readiness recheck: exact GHA Editor `6000.3.11f1` reported ready; `recompile_status`
returned `up_to_date`, `failed=false`, no errors; Console since cursor 493 returned no errors,
cursor 494, no dropped entries. No Play Mode transition or asset/settings change occurred.

### Remaining A5 PC Link baseline procedure

The earlier explicit Play Mode authorization covered Desktop checks. Agent-controlled PC Link
entry/exit requires the owner's explicit approval under AGENTS.md and a ready headset.
Record the headset/runtime, project revision, starting platform/XR settings, each observation and
Console errors. Preserve avatar preferences and restore temporary settings afterward.

1. While stopped, confirm Windows target, VertexForm VR mode, OpenXR initialization and the
   owner's working PC Link connection. Start from LoginScene and use the normal login route.
2. In Home, check head/controller tracking, stable environment, eye height/feet and mirror body.
   Check that first-person head/body visibility is appropriate and no duplicate saved body appears.
3. Prior edit/Save verification accepted September 26; do not repeat this step for the unchanged baseline. After relevant changes, check Classic/UMA selection, body-type changes and repeated Save/rebuild.
   Confirm hands keep following controllers and visibility survives reconstruction.
4. Briefly lose/resume tracking and confirm recovery. Record any failure rather than silently
   expanding this baseline into a fix; broader world/seating/network cases retain their later gates.
5. Stop, restore Desktop configuration, restart from LoginScene and check idle/moving arms are
   driven by animation rather than stale VR tracking. BUG-3 diagnosis remains deferred if seen.
6. Stop and restore original configuration/preferences. Record the owner's observations and
   diagnostics. Close A5 only when these results are available; do not infer them from Editor state.

## First implementation slice after baseline review

Extract a host-owned Home lifecycle controller and Classic apply path. Move the sole saved-avatar
listener out of `UmaHomeAvatar`; keep preview selection distinct from saved-body ownership. UMA
supplies construction, payload decoding and rebuild behavior through the smallest contract needed
by those call sites. Add host installer wiring and migrate existing serialized configuration through
Unity while preserving GUIDs and saved choices. Do not bundle package moves or an upstream upgrade.

Follow with runtime registry/player orchestration, explicit missing-provider behavior that preserves
saved selections, installer separation and a non-UMA Humanoid proof. Preserve mode IDs 0/1, recipe
v2/v1 reading, catalog IDs and Fusion layout. Actual network Save must be traced and tested.

The first slice must repeat the above Home cases and establish Classic Save without an UMA listener.
A genuinely UMA-free validation copy remains necessary before declaring host independence.

Documentation reconciliation check: all 11 review documents reflect the phase decisions and their
applicable package/static-avatar implications. All 45 stable tracking IDs remain, local Markdown
file links resolve, and the documentation whitespace check passes. Rechecking the original
nonsensitive inventory found all 88 non-document files unchanged; credential-bearing files were
not read or hashed. The staged scope remains the same 11 documentation files listed below.

## Local documentation checkpoint — commit awaiting owner review

The owner authorized staging exactly the following 11 files for review, including their existing
September 23 edits, this pass's evidence and the walking deferral. Creating the proposed
**one local documentation-only commit** requires separate approval after that review. No commit,
push, PR, branch creation, tag or history rewrite is authorized by the staging instruction.

1. `GHA-IMPLEMENTATION-PLAN.md`
2. `GHA-MIGRATION-RUNBOOK.md`
3. `GHA-SEPARATION-BASELINE-2026-09-23.md`
4. `GHA-developer-getting-started.md`
5. `GHA-getting-started.md`
6. `README.md`
7. `Packages/com.vertexform3d.gha/Documentation~/ARCHITECTURE.md`
8. `Packages/com.vertexform3d.gha/Documentation~/PACKAGE-BOUNDARIES.md`
9. `Packages/com.vertexform3d.gha.uma/Documentation~/SETUP.md`
10. `Packages/com.vertexform3d.gha.uma/README.md`
11. `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/README.md`

Excluded: credentials/local agent settings, all package/settings/import deltas, all scene/prefab
changes, ignored vendor content, generated indexes/caches, screenshots/logs, authoring helpers,
unrelated owner content and every path outside this explicit list. The local evidence directory
is ignored and not a checkpoint candidate. Commit approval is required by AGENTS.md, independently
of staging permission and the owner's direction that any commits remain local.

## Appendix A — exact pre-existing working-tree paths

Status is Git porcelain (M modified, D deleted, ?? untracked). These are pre-existing entries, not proposed changes from this pass.

| Status | Path | Classification |
| --- | --- | --- |
| M | `.gitignore` | unrelated-local-excluded |
| M | `Assets/MRTemplateAssets/Scenes/MixedReality/ReflectionProbe-0.exr.meta` | import-metadata-excluded |
| M | `Assets/MRTemplateAssets/Scenes/SampleScene/ReflectionProbe-0.exr.meta` | import-metadata-excluded |
| M | `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` | sensitive-local-excluded |
| M | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/README.md` | planning-docs |
| M | `Assets/VertexForm3D/Example_Assets/3d Model/Watch/digital-7 (mono) SDF.asset` | unrelated-local-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Environment/Textures/CloudSphere.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Global/RendererData/UniversalRP-HighQuality.asset` | unrelated-local-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-0_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-0_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-10_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-10_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-11_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-11_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-12_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-12_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-13_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-13_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-14_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-14_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-15_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-15_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-16_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-16_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-17_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-17_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-18_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-18_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-19_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-19_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-1_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-1_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-20_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-20_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-21_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-21_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-22_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-22_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-23_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-23_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-24_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-24_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-2_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-2_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-3_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-3_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-4_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-4_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-5_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-5_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-6_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-6_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-7_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-7_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-8_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-8_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-9_comp_dir.png.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/Lightmap-9_comp_light.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/ReflectionProbe-0.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/ReflectionProbe-1.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/Games/XRI_Examples/Scenes/XRI_Examples_Main/ReflectionProbe-2.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/MaterialExamples/Universal RP/14.0.8/URP Package Samples/Shaders/Lit/Lit/ReflectionProbe-0.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Example_Assets/MaterialExamples/Universal RP/14.0.8/URP Package Samples/Shaders/Lit/Lit/ReflectionProbe-1.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Resources/NewGenericMRDesktopPrefab.prefab` | webgl-serialization-excluded |
| M | `Assets/VertexForm3D/Scenes/Vertex Form 3D Scenes/Database Scenes/Geospatial/Mars Terrain/ReflectionProbe-0.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Scenes/Vertex Form 3D Scenes/Database Scenes/Other Scenes/Dark Space/ReflectionProbe-0.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Scenes/Vertex Form 3D Scenes/addressableScene/ReflectionProbe-0.exr.meta` | import-metadata-excluded |
| M | `Assets/VertexForm3D/Scripts/Data/Platforms.asset` | owner-settings-excluded |
| D | `Assets/VertexForm3D/Scripts/Editor/Vertexform3d Editor/VertexForm3DPackageExporter.cs.meta` | unrelated-local-excluded |
| M | `Assets/VertexForm3D/URP Profiles/UniversalRP-WebGL.asset` | owner-settings-excluded |
| M | `Assets/VertexForm3D/URP Profiles/UniversalRenderPipelineGlobalSettings.asset` | owner-settings-excluded |
| M | `Assets/XR/Settings/OpenXR Package Settings.asset` | owner-settings-excluded |
| M | `GHA-IMPLEMENTATION-PLAN.md` | planning-docs |
| M | `GHA-MIGRATION-RUNBOOK.md` | planning-docs |
| M | `GHA-developer-getting-started.md` | planning-docs |
| M | `GHA-getting-started.md` | planning-docs |
| M | `Packages/com.vertexform3d.gha.uma/Documentation~/SETUP.md` | planning-docs |
| M | `Packages/com.vertexform3d.gha.uma/README.md` | planning-docs |
| M | `Packages/com.vertexform3d.gha/Documentation~/ARCHITECTURE.md` | planning-docs |
| M | `Packages/com.vertexform3d.gha/Documentation~/PACKAGE-BOUNDARIES.md` | planning-docs |
| M | `Packages/manifest.json` | owner-package-change |
| M | `Packages/packages-lock.json` | owner-package-change |
| M | `ProjectSettings/EditorSettings.asset` | owner-settings-excluded |
| M | `ProjectSettings/GraphicsSettings.asset` | owner-settings-excluded |
| M | `ProjectSettings/ProjectSettings.asset` | owner-settings-excluded |
| M | `ProjectSettings/QualitySettings.asset` | owner-settings-excluded |
| M | `README.md` | planning-docs |
| ?? | `.claude/settings.local.json` | sensitive-local-excluded |
| ?? | `Assets/Settings.meta` | owner-settings-excluded |
| ?? | `Assets/Settings/Build Profiles.meta` | owner-settings-excluded |
| ?? | `Assets/Settings/Build Profiles/Meta Quest.asset` | owner-settings-excluded |
| ?? | `Assets/Settings/Build Profiles/Meta Quest.asset.meta` | owner-settings-excluded |
| ?? | `Assets/UMAProjectData.meta` | generated-excluded |
| ?? | `Assets/UMAProjectData/Tasks.meta` | generated-excluded |
| ?? | `Assets/VertexForm3D/Example_Assets/Ocean Villa/Textures/diet-healthy-nutrition-fresh-yellow-fruits-juice-background-texture-orange-water-bubbles-macro-41228705_tint0xF4B81CFF.jpg` | unrelated-local-excluded |
| ?? | `Assets/VertexForm3D/Example_Assets/Ocean Villa/Textures/diet-healthy-nutrition-fresh-yellow-fruits-juice-background-texture-orange-water-bubbles-macro-41228705_tint0xF4B81CFF.jpg.meta` | unrelated-local-excluded |
| ?? | `CLAUDE.md` | unrelated-local-excluded |
| ?? | `Tools/GenerateAvatarStudioCategoryIcons.ps1` | unreviewed-local-tool |
| ?? | `Tools/GenerateAvatarStudioSwatchLighting.ps1` | unreviewed-local-tool |
| ?? | `Tools/Keep-UnityCompileForeground.ps1` | unreviewed-local-tool |
| ?? | `vertexform3d-unity-vr-starterkit-GHA.slnx` | generated-excluded |

## Appendix B — exact committed delta from upstream

Ownership is the proposed destination. Mixed/review paths require responsibility or publication-scope review before extraction. Metadata follows its owning asset. This table does not authorize moving or exporting files.

| Path | Proposed owner |
| --- | --- |
| `.gitignore` | mixed-or-review |
| `AGENTS.md` | mixed-or-review |
| `Assets/VertexForm3D/3rdPartyAssets/GHA.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Generated.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Generated/GHAAvatarPanel.prefab` | mixed-or-review |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Generated/GHAAvatarPanel.prefab.meta` | mixed-or-review |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor/GhaIntegrationBootstrap.cs` | mixed-or-review |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor/GhaIntegrationBootstrap.cs.meta` | mixed-or-review |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor/GhaUmaAssetInstaller.cs` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor/GhaUmaAssetInstaller.cs.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor/GhaVertexFormAssetInstaller.cs` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor/GhaVertexFormAssetInstaller.cs.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor/Patches.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor/Patches/VertexFormGhaHost.patch` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Installer/Editor/Patches/VertexFormGhaHost.patch.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/README.md` | mixed-or-review |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/README.md.meta` | mixed-or-review |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Diagnostics.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Diagnostics/AvatarLoadProfilerCapture.cs` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Diagnostics/AvatarLoadProfilerCapture.cs.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Editor.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Editor/AvatarLoadProfileReportGenerator.cs` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Editor/AvatarLoadProfileReportGenerator.cs.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/RemoteAvatarProximityHider.cs` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/RemoteAvatarProximityHider.cs.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaAvatarBridge.cs` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaAvatarBridge.cs.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaAvatarPuppet.cs` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaAvatarPuppet.cs.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaHomeAvatar.cs` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaHomeAvatar.cs.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/UI.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/UI/UmaAvatarConfigurationProvider.cs` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/UI/UmaAvatarConfigurationProvider.cs.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/UI/UmaAvatarCustomizer.cs` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/UI/UmaAvatarCustomizer.cs.meta` | uma-integration |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/AvatarExtensionSync.cs` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/AvatarExtensionSync.cs.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/VertexFormHumanoidRigInput.cs` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/VertexFormHumanoidRigInput.cs.meta` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/VertexFormStockAvatarConfigurationProvider.cs` | host |
| `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/VertexFormStockAvatarConfigurationProvider.cs.meta` | host |
| `Assets/VertexForm3D/Resources/CustomEditor/HomeSceneComponent.prefab` | mixed-or-review |
| `Assets/VertexForm3D/Resources/GHA.meta` | host |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio.meta` | host |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/CategoryIcons.meta` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/CategoryIcons/Body.png` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/CategoryIcons/Body.png.meta` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/CategoryIcons/Colors.png` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/CategoryIcons/Colors.png.meta` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/CategoryIcons/Face.png` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/CategoryIcons/Face.png.meta` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/CategoryIcons/Outfits.png` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/CategoryIcons/Outfits.png.meta` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/SwatchCircle.png` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/SwatchCircle.png.meta` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/SwatchLighting.png` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/SwatchLighting.png.meta` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/SwatchRing.png` | uma-integration |
| `Assets/VertexForm3D/Resources/GHA/AvatarStudio/SwatchRing.png.meta` | uma-integration |
| `Assets/VertexForm3D/Resources/NewGenericMRDesktopPrefab.prefab` | mixed-or-review |
| `Assets/VertexForm3D/Resources/NewGenericMRVRPrefab.prefab` | mixed-or-review |
| `Assets/VertexForm3D/Scenes/Vertex Form 3D Scenes/HomeScene.unity` | mixed-or-review |
| `Assets/VertexForm3D/Scripts/AddressablesSystem/AddressablesDownloader.cs` | host |
| `Assets/VertexForm3D/Scripts/AvatarScripts/AvatarInputConverter.cs` | host |
| `Assets/VertexForm3D/Scripts/AvatarScripts/AvatarSelectionScripts/AvatarSelectionManager.cs` | host |
| `Assets/VertexForm3D/Scripts/Data/SerializedDataBase.cs` | host |
| `Assets/VertexForm3D/Scripts/Multiplayer/PlayerNetworkSetup.cs` | host |
| `Assets/VertexForm3D/Scripts/Multiplayer/SitSpot.cs` | host |
| `Assets/VertexForm3D/Scripts/Utility/SceneLoader.cs` | host |
| `Assets/XR/Settings/OpenXR Package Settings.asset` | mixed-or-review |
| `GHA-IMPLEMENTATION-PLAN.md` | mixed-or-review |
| `GHA-MIGRATION-RUNBOOK.md` | mixed-or-review |
| `GHA-SESSION-HANDOFF-2026-08-25.md` | mixed-or-review |
| `GHA-UMA-PERFORMANCE-BASELINE-2026-09-04.md` | mixed-or-review |
| `GHA-developer-getting-started.md` | mixed-or-review |
| `GHA-getting-started.md` | mixed-or-review |
| `Packages/com.vertexform3d.gha.uma/CHANGELOG.md` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/CHANGELOG.md.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Documentation~/SETUP.md` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Editor.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/LICENSE.md` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/LICENSE.md.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/README.md` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/README.md.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/Data.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.cs` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.cs.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/Recipes.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/Recipes/UmaRecipeCodec.cs` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/Recipes/UmaRecipeCodec.cs.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/Recipes/UmaRecipeStore.cs` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/Recipes/UmaRecipeStore.cs.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/UI.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/UI/UMADNASlider.prefab` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/UI/UMADNASlider.prefab.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/UI/UmaCustomizerRow.cs` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/UI/UmaCustomizerRow.cs.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/VertexForm.GHA.UMA.Runtime.asmdef` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/Runtime/VertexForm.GHA.UMA.Runtime.asmdef.meta` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/package.json` | uma-integration |
| `Packages/com.vertexform3d.gha.uma/package.json.meta` | uma-integration |
| `Packages/com.vertexform3d.gha/CHANGELOG.md` | host |
| `Packages/com.vertexform3d.gha/CHANGELOG.md.meta` | host |
| `Packages/com.vertexform3d.gha/Documentation~/ARCHITECTURE.md` | host |
| `Packages/com.vertexform3d.gha/Documentation~/ARCHITECTURE.md.meta` | host |
| `Packages/com.vertexform3d.gha/Documentation~/PACKAGE-BOUNDARIES.md` | host |
| `Packages/com.vertexform3d.gha/Documentation~/REFERENCE-REVIEW.md` | host |
| `Packages/com.vertexform3d.gha/Documentation~/REFERENCE-REVIEW.md.meta` | host |
| `Packages/com.vertexform3d.gha/LICENSE.md` | host |
| `Packages/com.vertexform3d.gha/LICENSE.md.meta` | host |
| `Packages/com.vertexform3d.gha/README.md` | host |
| `Packages/com.vertexform3d.gha/README.md.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animation.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animation/HumanoidLocomotionDriver.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animation/HumanoidLocomotionDriver.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animation/HumanoidPostureAnimator.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animation/HumanoidPostureAnimator.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Armature.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Armature.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/BasicMotions@SitGround01.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/BasicMotions@SitGround01.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/BasicMotions@SitHigh01.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/BasicMotions@SitHigh01.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/BasicMotions@SitLow01.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/BasicMotions@SitLow01.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/BasicMotions@SitMed01.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/BasicMotions@SitMed01.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/GHA_Locomotion.controller` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/GHA_Locomotion.controller.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/GHA_Locomotion_v2.controller` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/GHA_Locomotion_v2.controller.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Idles.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Idles.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Jump--InAir.anim.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Jump--InAir.anim.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Jump--Jump.anim.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Jump--Jump.anim.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Locomotion--Run_N.anim.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Locomotion--Run_N.anim.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Locomotion--Run_N_Land.anim.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Locomotion--Run_N_Land.anim.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Locomotion--Walk_N.anim.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Locomotion--Walk_N.anim.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Locomotion--Walk_N_Land.anim.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Locomotion--Walk_N_Land.anim.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Locomotion.controller` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Locomotion.controller.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Runs.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Runs.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Stand--Idle.anim.fbx` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/Stand--Idle.anim.fbx.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/StarterAssetsThirdPerson.controller` | host |
| `Packages/com.vertexform3d.gha/Runtime/Animations/StarterAssetsThirdPerson.controller.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Calibration.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Calibration/VrCalibrationSession.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Calibration/VrCalibrationSession.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Calibration/VrHeightCalibration.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Calibration/VrHeightCalibration.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Core.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Core/AvatarProviderSelection.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Core/AvatarProviderSelection.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Core/HumanoidAvatarContracts.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Core/HumanoidAvatarContracts.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Diagnostics.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Diagnostics/AvatarLoadTimingLog.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Diagnostics/AvatarLoadTimingLog.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Embodiment.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Embodiment/HumanoidArmRig.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Embodiment/HumanoidArmRig.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Embodiment/HumanoidSeatedPelvisAlignment.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Embodiment/HumanoidSeatedPelvisAlignment.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Embodiment/HumanoidVrAlignment.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Embodiment/HumanoidVrAlignment.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Networking.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Presentation.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Presentation/AvatarCameraVisibilityController.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Presentation/AvatarCameraVisibilityController.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Presentation/HumanoidAvatarPresentationController.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Presentation/HumanoidAvatarPresentationController.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Resources.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Resources/GHA.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Resources/GHA/AvatarStudio.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Resources/GHA/AvatarStudio/Chrome.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Resources/GHA/AvatarStudio/Chrome/RoundedBorder.png` | host |
| `Packages/com.vertexform3d.gha/Runtime/Resources/GHA/AvatarStudio/Chrome/RoundedBorder.png.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Resources/GHA/AvatarStudio/Chrome/RoundedPanel.png` | host |
| `Packages/com.vertexform3d.gha/Runtime/Resources/GHA/AvatarStudio/Chrome/RoundedPanel.png.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Tracking.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Tracking/HumanoidTrackingLifecycle.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Tracking/HumanoidTrackingLifecycle.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/Tracking/XrRigStartupStabilizer.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/Tracking/XrRigStartupStabilizer.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/UI.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/UI/AvatarConfigurationPanel.cs` | host |
| `Packages/com.vertexform3d.gha/Runtime/UI/AvatarConfigurationPanel.cs.meta` | host |
| `Packages/com.vertexform3d.gha/Runtime/VertexForm.GHA.Runtime.asmdef` | host |
| `Packages/com.vertexform3d.gha/Runtime/VertexForm.GHA.Runtime.asmdef.meta` | host |
| `Packages/com.vertexform3d.gha/package.json` | host |
| `Packages/com.vertexform3d.gha/package.json.meta` | host |
| `Packages/manifest.json` | mixed-or-review |
| `Packages/packages-lock.json` | mixed-or-review |
| `ProjectSettings/ProjectSettings.asset` | mixed-or-review |
| `ProjectSettings/TagManager.asset` | mixed-or-review |
| `README.md` | mixed-or-review |
| `Tools/AuditUmaLoadIndex.cs` | uma-integration |
| `Tools/CreateAvatarStudioChrome.cs` | mixed-or-review |
| `Tools/EnableUmaHumanBodyTypes.cs` | uma-integration |
| `Tools/InspectUmaHumanTypes.cs` | uma-integration |
| `Tools/InspectUmaLoadSamples.cs` | uma-integration |
| `Tools/PlanUmaStreaming.cs` | uma-integration |
| `Tools/ProfileUmaLogin.cs` | uma-integration |
| `Tools/Sync-Uma.ps1` | uma-integration |
| `Tools/ValidateAvatarStudioChrome.cs` | mixed-or-review |
| `Tools/ValidateAvatarStudioHeader.cs` | mixed-or-review |
| `Tools/ValidateAvatarStudioSlotSelection.cs` | mixed-or-review |
| `Tools/ValidateGhaHostInstaller.cs` | mixed-or-review |
| `Tools/ValidateUmaHumanBodyTypes.cs` | uma-integration |
| `Tools/ValidateUmaIndexInitialization.cs` | uma-integration |
| `Tools/ValidateUmaInstallerReferences.cs` | uma-integration |
