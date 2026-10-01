# QuantumVertex to Generic Humanoid Avatars Migration Runbook

Planning revision: 2026-10-01 (session close; performance deferred until after UMA 3.1); dated verification entries below retain their original scope.
Migration source: `E:\src\Unity\6000.3\QuantumVertex`  
Migration target: `E:\src\Unity\6000.3\vertexform3d-unity-vr-starterkit-GHA`

## Purpose

This is the controlling implementation and handoff document for migrating the avatar work from
QuantumVertex into the public VertexForm3D `Generic-Humanoid-Avatars` branch.

It reconciles the original QuantumVertex plan with the partial migration already present in the
target repository. A new agent must use this document to determine:

- what is authoritative;
- what has merely been copied;
- what remains unfinished from the original plan;
- which package or repository owns each change;
- which verification gate must pass before moving to the next phase.

Do not interpret the presence of a file in the target as proof that the behavior was migrated
correctly. The current target is a migration workbench, not an accepted baseline.

## Status vocabulary

| Status | Meaning |
|---|---|
| `DONE` | Implemented and verified for the stated scope. |
| `PROVISIONAL` | Present in the target, but parity or runtime behavior has not been accepted. |
| `BLOCKED` | Work cannot be validated until the stated blocker is resolved. |
| `PENDING` | Required work that belongs in the active migration. |
| `DEFERRED` | Intentionally retained in the roadmap but not required for the next migration gate. |
| `NOT STARTED` | Required original-plan work with no accepted implementation. |
| `SUPERSEDED` | Replaced by a documented design or implementation decision. |

## Read first

Target repository:

1. `AGENTS.md`
2. `GHA-SESSION-HANDOFF-2026-08-25.md`
3. This runbook
4. `Packages/com.vertexform3d.gha/Documentation~/PACKAGE-BOUNDARIES.md`
5. `Packages/com.vertexform3d.gha/Documentation~/ARCHITECTURE.md`
6. `Packages/com.vertexform3d.gha/Documentation~/REFERENCE-REVIEW.md`
7. `Packages/com.vertexform3d.gha.uma/Documentation~/SETUP.md`
8. `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/README.md`

QuantumVertex source references:

1. `V2-PLAN.md`
2. `AVATAR-FRAMEWORK-HANDOFF-2026-07-12.md`
3. `Assets/QVAddons/AvatarSuite/ARCHITECTURE.md`
4. `Assets/QVAddons/AvatarSuite/REFERENCE-REVIEW.md`
5. `UMA-UPGRADE-DELTA-ANALYSIS-2026-07-24.md`

When documentation and live source disagree, do not silently choose one. Record the discrepancy in
the migration ledger and establish the frozen QuantumVertex commit before porting the affected
slice.

## Non-negotiable rules

1. QuantumVertex becomes read-only after its current functional avatar state is checkpointed and
   committed. Avatar timing analysis and performance optimization are explicitly deferred until
   the full functional migration is complete. Until the checkpoint commit exists, the target
   cannot claim source parity.
2. Migrate by responsibility and verification slice, not by bulk folder replacement.
3. Keep these change sets independently reviewable:
   - minimal provider-neutral VertexForm3D core seams;
   - `com.vertexform3d.gha`;
   - `com.vertexform3d.gha.uma`;
   - host-coupled staging adapters;
   - scene, prefab, Animator, and project configuration;
   - diagnostics and tests.
4. `Assets/UMA` and `Assets/SourceShaders` are pristine, pinned third-party UMA vendor content
   plus documented pruning/compatibility corrections. They are ignored and never committed as
   part of GHA.
5. Do not copy or merge UMA's generated `AssetIndexer.asset`; rebuild the Global Library.
6. Do not commit Photon credentials or other machine/deployment credentials.
7. Preserve Unity `.meta` files when moving existing Unity assets. Perform scene, prefab,
   ScriptableObject, and Animator work through the Unity editor tooling.
8. Always enter Play Mode from `LoginScene`.
9. Do not submit an upstream pull request until the migration, validation matrix, and commit
   separation are complete. The current branch is not PR-ready.
10. This is a public repository. Never stage, commit, push, or open/update a pull request without
    the user's express approval for that specific Git action and its exact reviewed file scope.
    Editing files or receiving general permission to continue the migration is not Git approval.

## Current repository truth

### October 1 session-close decision — controlling next-work direction

The owner accepted the improvement from the smaller index and directed that **further performance
work wait until after the UMA 3.1 upgrade**. E3 / R9 / BUG-9 is therefore **DEFERRED**, not fixed
or accepted. This decision supersedes immediate profiling/loading recommendations in the dated
investigation entries below. Do not initiate more performance captures, background loading,
Addressables changes or index optimization before P5-4.

| Reference | Current disposition | Next action |
| --- | --- | --- |
| D1 / Phase 1 | Source extraction and bounded Desktop Home construction verified; full acceptance incomplete | Resume separation/lifecycle work on the existing pinned UMA version. Preserve phase review boundaries and the original tracking IDs. |
| E3 / R9 / BUG-9 | Deferred until after P5-4 | Preserve the scoped index locally and the experiment source/evidence; reassess performance after upgrading. UMA 3.1 is not assumed to cure the remaining stalls. |
| P5-4 | Final UMA 3.1 upgrade remains pending | Finish the current-version separation, review the chosen released patch, upgrade and run required regressions before additional testers/public release. |
| C2 / handoff | Owner authorized local checkpoints on the current branch October 1 | Implementation saved as `1e0cec7d`; companion documentation checkpoint replaces the earlier 11-file staged snapshot. See handoff for 14 implementation/tooling files, 12 documents and excluded local files. No push authorized or performed. |

The smaller index improved the measured initial interval from 5.57 s to 0.56 s, but the single
fresh-Editor pair still measured 7.19 s versus 6.47 s to Home avatar completion. The 24 wardrobe
choices reference a larger dependency set, including 122 texture paths and both races; they are
not 24 independent small assets. Remaining generation/rendering cost in the scoped run is not
fully attributed. Do not generalize the pair to VR/device performance or promise background work
will be stall-free.

The current [session handoff](GHA-SESSION-HANDOFF-2026-08-25.md) contains the itemized machine-transfer
checklist, source inventory and setup caveats. It supersedes the August 25 environment snapshot.
The original project is the continuation checkout; the September 24 comparison worktree is
historical evidence. No further Unity execution is needed for this documentation closeout.

### October 1 VR first-load investigation — 24.6 s Home build, 0.60 s world build

#### Catalog-scoped index experiment — October 1, 15:37–15:45 UTC

Owner authorized trying the resource list limited to current UI choices and asked whether
additional content could load in the background after avatar appearance. A slimmer project-owned
index is now active locally. **This is a synchronous-loading experiment, not activated streaming
or a release-ready build configuration.** No clothing choice or saved recipe was removed.

`Tools/CreateCatalogScopedUmaIndex.cs` rebuilds fresh entries from the current catalog's races,
wardrobe recipes, named slot/overlay/LOD dependencies and serialized dependency closure, using
Unity/UMA APIs. It creates `Assets/UMAProjectData/Resources/AssetIndexerProject.asset` and refuses
to replace an existing project index. It does not clone the generated vendor index. The full
vendor index and catalog remain byte-identical to their before-test hashes. The generated
project index is ignored by Git; the reproducible creation script is new and unstaged.

| Scope | Full index | Catalog-scoped index |
| --- | ---: | ---: |
| Persisted records when created | 513 (508 persistent references) | 100 |
| Dependency paths | 1,377 | 468 |
| Texture dependency paths | 464 | 122 |
| UI choices retained | Both races, 24 wardrobe entries | Both races, all 24 wardrobe entries |

Both timed runs used the original E: project, today's runtime code, Unity 6000.3.11f1,
Desktop/StandaloneWindows64, the same saved male recipe and the same temporary Editor-only
test helper. Each began after restarting Unity, with zero loaded indexes before normal
Connect Anonymously -> HomeScene. No binary profiling or Editor query ran during construction.
The machine/OS caches were not flushed; these are two measured runs, not a statistical benchmark.

| Measured interval | Full, fresh Editor | Scoped, fresh Editor |
| --- | ---: | ---: |
| DCA initialized -> build begun | 5.5661 s | 0.5581 s |
| Home avatar START -> FINISH | 7.1900 s | 6.4740 s |
| Largest observed Home-loading frame | 6.9267 s | 1.9210 s |

Frame values are observed `Time.unscaledDeltaTime` through three frames after Home FINISH,
not a raw-profiler main-thread attribution or headset measurement. The scoped run's later
generation interval was longer, so total ready time improved only modestly despite the much
shorter initial load. Significant stalls remain; do not claim a smooth or 1-second cold load.
Only the project index was resident. Matching loaded textures measured 691,340,056 bytes
(119 Texture2D objects), versus 1,281,504,424 bytes in the earlier full-index residency audit;
these are Editor memory estimates. Five additional baked slot records were transient at runtime.

Runtime coverage passed 31 cases: male base plus 17 compatible wardrobe choices, female base
plus 12 compatible choices (including shared hair). Each built a fresh puppet, checked readiness,
Humanoid Animator, nonempty renderer meshes, assigned wardrobe and absence of errors. This
tests construction, not every outfit combination, visual/material correctness, networking,
VR, or standalone platforms. The saved recipe was unchanged; all test objects were removed.

Background-loading direction: first load the selected avatar, then queue useful supported
outfits/other-player assets at low priority with bounded concurrency and memory retention.
Do not automatically load unused vendor content simply because it is installed. UMA's existing
Addressables preload uses race and assigned recipe labels, but enabling it needs local bundles,
reference-graph ownership and build validation. Async reads still require main-thread integration
and GPU work. Unity's background-loading priority applies only to built players, so Editor tests
cannot prove that background work will be imperceptible:
https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-backgroundLoadingPriority.html

The full Resources index still exists for preservation and would remain eligible for inclusion
in a player build. Resolve authoring/runtime index ownership and build duplication before release.
A generic UMA library rebuild can repopulate a curated index; this experiment does not yet add
an automatic policy that regenerates the curated set when the catalog changes. The creation
script and its validation must become an intentional installer/build step before broader use.

Evidence: `Logs/ScopedUmaIndex-20261001/` contains both timing logs, Editor logs, creation audit,
residency, 31-case validation and helper source. Temporary helper scripts/metas were removed
through Unity. The project is stopped at LoginScene with profiling disabled. Current runtime
source, vendor index, catalog and saved recipe are preserved. No new staging, commit or push.
CLI inspection again timed out; fresh AnkleBreaker discovery selected the exact GHA project.
All four required VertexForm documentation URLs were retried but remained inaccessible.

#### Cause isolation — October 1, 15:17–15:25 UTC

**The large pre-build delay is synchronous loading of UMA's index and its directly referenced
assets, not slow lookup-table initialization.** Normal application flow remains Connect
Anonymously in LoginScene -> HomeScene -> avatar startup. The test logs confirm zero loaded
indexes before Connect and avatar timing events in HomeScene. “Login test” describes the entry
path, not an avatar in LoginScene.

The source path is `DynamicCharacterAvatar.Start` -> `BuildFromComponentSettings` ->
`SetActiveRace` -> the `RaceSetter.data` getter -> `SetRaceData` -> `UMAAssetIndexer.Instance`.
The getter consults the index even though the host already supplied RaceData. In the Editor,
`LoadPreferredIndexer` uses synchronous AssetDatabase loading; the player fallback uses
synchronous Resources loading. Each `AssetItem._SerializedItem` is a direct object reference.
Resolving this global index therefore pulls in much more than the selected avatar's outfit.

A temporary Editor-only probe intercepted HomeScene's scene-loaded event before avatar Start.
It loaded the existing index, measured singleton initialization separately, then let the
unchanged Home/runtime code continue. No avatar existed at interception; no index was loaded.
The test stayed on the original E: project, current code, current index, same saved recipe,
Desktop/Windows, with no binary profiler or Editor queries during construction.

| Measured step | Time / evidence |
| --- | --- |
| Load the index asset and its references, in HomeScene before avatar Start | 5,630.154 ms |
| Obtain the singleton and initialize UMA after that load | 18.907 ms; same index object |
| Normal Home avatar START -> FINISH after the probe | 1,421.6 ms; SUCCESS, zero errors, two renderers |
| DCA initialized -> build begun with the index already initialized | 13.8 ms, versus seconds in unprimed runs |
| Separate index-only load after an Editor restart, stopped Edit Mode | 4,379.614 ms; loaded Texture2D count increased by 242 |

This deliberately moves the loading cost ahead of the avatar timer; **it is causal isolation,
not a performance fix or a 1.42-second total Home load**. The earlier profiler captures still
establish substantial render-thread texture-upload stalls and main-thread waits in slower
runs. Do not add overlapping thread timings. A normal current-code control after unloading
unused assets took 3.8588 s in the already-used Editor. An Edit-Mode preload attempt took
4.3081 s in Home but had zero indexes left before Connect: entering Play unloaded the index,
so that attempt is explicitly not a valid preloaded control.

The read-only dependency audit found 513 index records / 508 persistent references, 1,377
dependency paths and 464 texture asset paths. Of the loaded Texture2D objects matching that
dependency set, 284 accounted for 1,281,504,424 bytes in Unity's runtime-memory estimate.
Five female 2048x2048 RGBA32 readable normal maps alone accounted for 223,700,680 bytes even
while investigating a male avatar. These are Editor residency estimates, not device VRAM or
standalone measurements; the dependency texture count is not the number of resident textures.
The 20 project-wide index entries absent from the historical rebuild add 76 unique dependency
paths but only two texture source files (260,663 bytes combined). The large UMA texture set is
shared, so those extra entries do not explain it. Other dependency costs are not ruled out.

Disk inspection confirms that C: is a WD_BLACK SN850X NVMe SSD and E: a Samsung 870 QVO SATA
SSD. That is an actual confound in the earlier worktree comparison, not proof of the cause of
its 7.3-second gap. OS cache, GPU state and exact September 24 cache state remain uncontrolled.
The same-project Home-code A/B did not reproduce a consistent extraction penalty; it does not
exclude every possible regression. Bulk index loading and texture uploads are now established
costs; exact attribution of every first-load difference remains open.

E3's corrective direction is a scoped runtime content/index graph and supported asynchronous
loading for the selected recipe, without hard references from the global Resources index or
catalog retaining all streamed content. The existing September 7 dry run identified a
100-record candidate for both catalog races and 24 wardrobe options; that remains an audit,
not validated pruning or an activated streaming implementation. Merely moving the synchronous
load into LoginScene, or making one API asynchronous while retaining the full reference graph,
does not address the excessive content load. Validate all customization options and device
first launch/reload/restart before calling a fix complete.

Evidence is in `Logs/September24Worktree-20261001/`: `home-index-isolation.txt`,
`index-alone-fresh-editor.json`, `current-dependency-audit.json`, `index-texture-residency.json`,
the two control logs, `HomeIndexIsolationTest.cs`, and `isolation-final-state.json`.
The probe and its meta were removed from Assets through Unity after the test. The original
project is stopped at LoginScene with profiling disabled. Guarded-file hashes differ only for
this runbook and the implementation plan; the vendor index hash is unchanged. No persistent
runtime, vendor/index, scene, prefab, package or settings change is part of this investigation.
The pre-existing staged scope was preserved; nothing was staged, committed or pushed.

#### Full historical-worktree Desktop tests — October 1, 14:14–14:30 UTC

At the owner's request, created the attached worktree
`C:/Users/Blender/.codex/worktrees/september-24-avatar-baseline/vertexform3d-unity-vr-starterkit-GHA`
at `8c19b5f773985b71045df92337fc107595512d56`, the commit recorded in the September 24 baseline.
This checks out all tracked historical code, unlike the earlier single-file Home comparison.
The owner closed today's project and explicitly authorized opening projects and automatic
Desktop Play Mode testing. No commits, staging or pushes were authorized or performed.

Preparation applied 73 settings/import/package files whose bytes match the September 23
inventory. UMA was installed from pinned upstream `c9204fe4` with the recorded 12 shader fixes
from `f4edf41ba`, using the checked-in sync script. Vendor comparison found only import-metadata
differences, not changed runtime source/content. Local Photon configuration was copied privately
for the test; its values and preference backups remain in ignored local evidence only.
The full original working-tree files were preserved; no reset or stash was used.

Both projects used Unity 6000.3.11f1, StandaloneWindows64, VertexForm Desktop, the same saved
male UMA recipe (SHA-256 `223CA93931326A912644D62C4FD76C0AC4E4EE17F82819D9C09A55A78E8ACEAE`),
and the same temporary Editor-only login/timing helper. Historical Standalone XR startup and
loaders were disabled through Unity APIs because the old SceneLoader otherwise initializes XR
even for Desktop. Today's Desktop gate already avoids that initialization. No headset was used.
After imports/setup each Editor was restarted before the measured first Play. No binary profiler
or Editor inspection ran during the avatar-build intervals. Normal timing logs were retained.

| Condition | Historical worktree | Today's project |
| --- | ---: | ---: |
| First Home avatar after Editor restart | 6.4558 s | 13.7469 s |
| Stop Play, then Login -> Home again in the same Editor process | 3.2765 s | 3.3710 s |
| Reload Home directly while remaining in that Play session; fresh Home puppet | 0.4611 s | 0.4658 s |

All six builds reported SUCCESS with zero errors during the recorded intervals and identical
saved-recipe hashes. First Play had zero loaded indexes before login; the next Play had one.
Same-Play reload is a separate diagnostic scene reload, not the full Login path. Both warm
reloads log `build=initial` for the new puppet, so these are not just timing an existing renderer.
The final current Home had one host, one puppet, two active/visible UMA renderers, no XR loader;
the saved game screenshot also confirms the avatar is visible.

**Finding:** both code versions retain very fast same-Play avatar construction. This supports
runtime cache/state as an explanation for how a roughly 1.2-second historical build can coexist
with much longer first loads, but does not prove the September 24 run had identical cache state.
The September 24 loading substep was about 9 ms; the historical same-Play reload here took about
2 ms for that substep. Stopping/restarting Play was slower than keeping the runtime alive in
both versions. Do not conflate those two meanings of a warm load.

**First-load gap remains OPEN.** Today's first load was approximately 7.3 s slower. Its DCA
initialized-to-build-begun interval was 12.121 s versus 2.759 s in the worktree. This is evidence
to preserve, not grounds to clear the regression or to attribute it to the separation code.
This was not an exact recreation: the managed worktree is on C: and today's project is on E:;
storage/cache conditions were not matched or flushed. Historical uncommitted state is only
partially recoverable from hashes, and its generated index was not archived.

The runbook forbids copying a generated index, so the historical worktree rebuilt one scoped to
`Assets/UMA`: 488 persistent entries / 1,301 dependencies. Today's existing index, after leaving
Play, has 513 records, 508 persistent references and five empty runtime-baked records. Its 488
UMA entries are shared with the rebuilt index; it also includes 20 persistent entries elsewhere
in the project (mostly Animator controllers, plus one SourceShaders material). The five extra
live baked entries in the in-Play export are runtime-only, not an additional persistent-content
delta. Today's index file hash is unchanged from before the earlier A/B work. The older
493/488 audit was September 4 evidence, not a verified September 24/current index snapshot.
Therefore equal persistent index content has NOT been established. A future first-load
attribution test must match index contents and storage/cache conditions before comparing code.

Evidence: `Logs/September24Worktree-20261001/` contains the six timing files, original-file hash
inventory, historical overlays, vendor/index comparison lists, Editor logs and
`current-desktop-home.png`. The temporary `HistoricalHomeTest.cs` source is retained there and
in the isolated worktree for reproducibility; it was removed from today's Assets through Unity.
Shared avatar/preferences values are unchanged; only four Unity session identifiers/counters
changed through normal Editor/Play sessions. Preference backups are retained privately.
Today’s original modified/untracked files matched their pre-test hashes before documentation
updates. The original project is back on Desktop / Windows target at LoginScene, stopped;
the historical Editor is closed. Keep the historical worktree attached for follow-up.

Unity CLI successfully opened both projects. Its installed Pipeline 0.6.0 status commands still
timed out after 30 s; freshly discovered AnkleBreaker endpoints were used for Editor operations
(historical worktree 7890, original project 7892). No Pipeline/package upgrade was attempted.

#### Historical regression review requested by the owner

The owner recalls improving approximately 30-second loads to below ten seconds and is concerned
the September 27 separation work reversed that improvement. Reviewed the earlier
[Resume GHA avatar migration](codex://threads/01a03925-40c7-70f2-bdaf-9d9888d88eff) session,
its August 25–September 23 history, retained timing evidence and the current source diff.
Do not dismiss this concern because slow-load behavior also existed before the split.

| Earlier observation | Evidence / qualification |
| --- | --- |
| September 4 Home ready in 7.720 and 8.291 s | `Logs/MetaOpenXR-Fix-2026-09-04/UMA-PERFORMANCE-BASELINE.md`; adjacent runs were 11.314/11.801 s, so performance varied before separation. |
| September 4 profile: Home ready in 21.457 s, full captured frame 25.289 s | `UMA-PROFILE-FINDINGS.md`; includes an 18.46 s synchronous index load. Full-frame time and avatar-ready time are different measurements. |
| September 7 review-project Home build about 8.3 s | Earlier session's installer-fix result at 23:37 UTC; explicitly said loading performance remained unresolved. Different project copy. |
| September 24 Home builds 1.309 s and 1.176 s | `Logs/SeparationBaseline-20260923/pc-link-20260924-visibility-console.json`; first interval is earlier activity, second is the PC Link baseline. The PC Link body then remained hidden for approximately 11.9 s pending calibration. Neither is a controlled cold-process benchmark. |

The reviewed earlier conversation confirms that the September 4/5 smaller-index and local
asynchronous-loading work stopped at a feasibility audit; it explicitly reported no changed
loading behavior. No activated streaming optimization was found to have been removed. This does
not disprove other earlier improvements or the owner's remembered approximately 30-second run.

The separation delta moves the existing Home XR wait, saved-avatar dispatch and presentation
into `VertexFormHomeAvatar`; `UmaHomeAvatar` now creates its puppet lazily in BuildSavedAvatar
rather than unconditionally in Start. For initial UMA, both still invoke the same puppet Build
and UMA DCA Start path. Puppet Awake resolves host components and resets locomotion; no asset
preload/cache was found there. `UmaAvatarPuppet`, the serialized Home prefab and UMA catalog
match HEAD, and EditorSettings/QualitySettings match the September 23 pre-work hashes exactly.
The customizer source delta only changes its missing-listener error text. These findings narrow
the suspects but cannot rule out altered startup ordering or timing from the extraction.

#### Controlled Home comparison completed October 1, 13:28–13:43 UTC

The owner authorized proceeding, including the previously authorized stop/start actions. Compared
the current Home implementation against `UmaHomeAvatar.cs` at `8c19b5f7`, temporarily replacing
only that source file. This isolates the Home lifecycle extraction; it is not a rollback of all
Phase 1 changes. Current source was backed up and restored byte-for-byte. No vendor, catalog,
prefab, recipe, presentation or XR settings were changed for the comparison.

Each run used Desktop from LoginScene, the same saved UMA recipe, a script-domain reload and
unused-asset unload. Before every run, inspection confirmed zero loaded UMAAssetIndexer objects.
Current runs had one VertexFormHomeAvatar and one puppet; pre-extraction runs had no new host
and one puppet. All five captures completed successfully. The Editor stayed in one process;
Windows storage caches were not flushed. These are unloaded-asset Editor repeats, not controlled
cold-process or standalone-headset benchmarks.

| Order / implementation | Home build request to ready | DCA synchronous `Loading.ReadObject` | Render-thread texture upload | Capture suffix |
| --- | ---: | ---: | ---: | --- |
| 1 — current | 11.565 s | 2.971 s | 8.061 s | `132843` |
| 2 — pre-extraction | 10.822 s | 2.359 s | 7.797 s | `133058` |
| 3 — pre-extraction | 12.953 s | 5.075 s | 7.447 s | `133617` |
| 4 — current, measurement-contaminated | 17.721 s | 7.011 s | 7.708 s | `133916` |
| 5 — current, no Editor query during loading | 10.355 s | 2.279 s | 7.370 s | `134238` |

Run 4 is retained rather than discarded silently: a premature capture-status query added
2.268 s of Editor bridge work in profiler frame 2 while the avatar was still building. Its slow
frame also contains a 3.234 s UMA Editor callback, mostly an AssetDatabase lock wait, and slower
synchronous reads. Do not subtract these measurements to manufacture a corrected result or
use this run as a clean comparison. Run 5 waited for the SUCCESS line in the disk log before
querying the Editor. Normal login tooling occurs in every captured startup frame; the largest
whole-Editor frame is not an avatar-only timing. Thread timings overlap and must not be summed.

All runs read approximately 1.269 GB across the capture. Matched first-pair captures differ by
only 528 bytes in total reads and both have 314 texture-upload calls in the slow frame; the
second pair has 415 calls each, reflecting different captured frame grouping. This does not
prove every uploaded texture belongs to UMA, but it gives no evidence of duplicated bulk asset
loading caused by the Home extraction.

**Finding:** no consistent Home-extraction slowdown was reproduced under these Desktop
conditions. Uncontaminated current results (10.4–11.6 s) overlap pre-extraction results
(10.8–13.0 s); this small same-process comparison cannot rule out a cold/VR regression or a
regression elsewhere in the separation work. E3 remains OPEN. The substantial synchronous
asset/reference load and texture upload are reproduced in both implementations. Next investigate
the index dependency/texture set before choosing prewarming, smaller startup content or async
loading. A device build must separately measure first launch, same-session reuse and app restart;
do not claim a persistent cache or a performance fix yet.

Evidence: `Logs/HomeAB-20261001/timing.txt`, `tool-results.json`, `before-hashes.json`,
`after-hashes.json`, source backups and staged-scope snapshots; raw captures and matching
`.analysis.txt` files are under `Library/GHAProfiles/20261001-<suffix>-*`. The five raw captures
are 62,058,005 / 62,144,723 / 61,044,576 / 87,915,262 / 60,797,970 bytes respectively.
The supported CLI timed out; fallback used freshly listed/selected AnkleBreaker instance 7896
for this exact project. Final state: current source restored, compilation has zero errors,
stopped in LoginScene, profiling stopped/disarmed. All 12 guarded source/meta/content/settings
hashes are unchanged. Existing staged scope remains unchanged; nothing staged, committed or
pushed by this comparison. No owner retest is needed to repeat these Editor observations.

Desktop comparison completed at 13:14 UTC. The owner set VertexForm presentation to Desktop
and disabled XR startup for both Windows and MetaQuest; restore the appropriate target's XR
startup before a later headset build/run. Raw file
`Library/GHAProfiles/20261001-131354-551-owner-login-20261001-131354.raw` is 225,755,587 bytes,
finished with SUCCESS, and has profiler indices 0–1505. The matching `-analysis.txt` covers
the slow avatar frame 1474; filtered events are in
`Logs/VRFirstLoad-20261001/captured-desktop-timing.txt`. Recorder is stopped and disarmed.

| Measurement | Captured VR repeat | Captured Desktop repeat |
| --- | --- | --- |
| Avatar build request to ready | 10,198.2 ms | 9,335.5 ms |
| Slow main-thread frame | 9,127.739 ms | 9,192.237 ms |
| DCA startup | 2,286.279 ms | 2,221.029 ms |
| DCA's synchronous asset-read subtree | 2,201.968 ms | 2,145.039 ms |
| Render-thread texture upload | 7,082.474 ms | 6,677.409 ms |

Desktop main-thread wait is 6,088.932 ms under `MainToolbarWindow.Paint > Semaphore.WaitForSignal`,
while the render thread uploads textures. VR's wait appears under XR frame submission instead.
The same broad loading/upload costs occur in both modes; XR is not required to reproduce this
Editor stall. Event-marker placement differs: Desktop reaches CharacterBegun in the same game
frame, with the long delay afterward; VR reaches that event on the following frame. The CPU
hierarchy prevents incorrectly attributing the Desktop interval entirely to character generation.
These are sequential captures in the same editor process, not controlled cold-start comparisons
or standalone-device evidence. No fix or claim of equivalent headset-build performance follows.
The next decisive performance check is a development build on the actual headset, including
first launch, same-session reload and app restart. No additional owner Editor repeat is needed
for this comparison. Left the current Desktop Home session running; no Git actions occurred.

Successful repeat capture at 13:08 UTC: raw file
`Library/GHAProfiles/20261001-130815-678-owner-login-20261001-130815.raw`, 86,346,453 bytes,
finished with SUCCESS and automatically disabled profiling/binary logging and disarmed.
Inspected while the owner remained in Home Play Mode; selected slow-frame report is the matching
`-analysis.txt`, with filtered events in `Logs/VRFirstLoad-20261001/captured-vr-timing.txt`.
The capture spans 461 profiler frames; the initial avatar took 10,198.2 ms. It is a repeat in
the same editor process, not a controlled fresh-editor or standalone cold-load measurement.

| Captured cost | Evidence |
| --- | --- |
| Longest measured main-thread frame | Profiler frame 428: 9,127.739 ms |
| DCA startup | 2,286.279 ms; `Loading.ReadObject` child 2,201.968 ms |
| Index identity | Raw sample 2585 metadata: `MonoBehaviour: AssetIndexer`, object `AssetIndexer`, serialized file index 10350, local identifier 11400000 |
| Main-thread render wait | `FrameEvents.XRBeginFrame > Gfx.WaitForRenderThread`: 6,771.186 ms |
| Render-thread work in the same frame | `Gfx.UploadTexture`: 7,082.474 ms across 231 calls; child `Gfx.UploadTextureData`: 6,957.935 ms across 2,497 calls |

The two threads overlap: do not add render-thread upload time to the main-thread wait as separate
wall-clock costs. This attributes the previously unexplained approximately 6.9-second gap in
this capture to texture upload/render synchronization, rather than a calibration timer or seven
seconds of avatar mesh generation. Exact texture identities and why all were resident remain
to be audited. The index load dropped from the earlier 13–16 seconds to 2.2 seconds; cache state
is a plausible contributor, not an isolated causal measurement. No runtime performance fix was
applied. Existing large library/reference loading remains relevant alongside GPU upload cost.

Stopped Play using prior authorization and verified LoginScene with the recorder rearmed for
the owner's Desktop comparison; platform selection is left to the owner. The owner proposes
an eventual build-and-run on the headset: treat development-player/device measurements as the
standalone gate and compare first launch, same-session reload, and a fresh app launch separately.
No build or platform-performance result is yet claimed. Current evidence shows same-session
reuse, not a persistent avatar cache guaranteeing fast startup after an app restart.

Capture follow-up result: the raw file `20261001-130054-860-owner-login-20261001-130054.raw`
is 24,218,652 bytes, but records only an aborted 3.53-second run ending with
`result=PLAY_MODE_ENDED`; it is **not** a CPU recording of the subsequent slow Home build.
The next run at 13:03–13:04 UTC was unarmed and its event logs show XR readiness 1,136.8 ms,
avatar build 21,792.9 ms, a 13,561.2 ms same-frame DCA startup gap and 7,166.2 ms from recipe
update to generation beginning. Evidence: ignored `Logs/VRFirstLoad-20261001/repeat-timing.txt`.
No CPU attribution was derived from the aborted file. The diagnostic's original one-shot
arming was consumed too early; this limitation was explained to the owner.

Using existing stop/restart authorization, stopped Play and corrected the diagnostic so
interrupted runs retain the arm request; only successful Home completion clears it. Verified
zero compile errors, stopped clean LoginScene, VR presentation, profiler currently off, and
`IsArmed=true`. The next owner VR attempt is ready; successful full recording and persistence
through an actual interrupted retry remain to be observed. CLI again timed out; the exact GHA
AnkleBreaker instance remained selected at port 7896. No new staging/commit/push occurred.

Follow-up capture preparation: the owner selected **VR first, then Desktop**. Added the explicit
Editor-only `GhaOneShotAvatarProfile` diagnostic: arming persists across the Play Mode domain
reload, starts the existing raw profiler on entry to Play, and restores profiler settings three
frames after the first Home avatar completes (or on timeout/Play exit). It does not log in or
change Play Mode itself. Compilation has zero errors, LoginScene is stopped, presentation is VR,
deep profiling is disabled, and `IsArmed=true` was verified. The next owner Play is ready to
capture; no new capture result is yet claimed. Primary CLI editor status also timed out, so
AnkleBreaker was used after listing and selecting the exact GHA instance on port 7896. The
diagnostic and metadata remain unstaged. Re-arm separately before Desktop; record editor/cache
history so a warm repeat is not mislabeled a controlled cold-start comparison.

The owner reports a 20–25-second initial VR avatar delay and headset hourglass, while head
movement continued; the subsequent Ocean Villa avatar appeared quickly. This explicitly
resumes targeted investigation of the observed delay, without upgrading UMA or changing the
agreed separation sequence. CLI Console requests timed out after 30 seconds, so evidence was
read from the local Unity `Editor.log`; no editor restart or Play Mode change was made.
Filtered evidence is retained in ignored `Logs/VRFirstLoad-20261001/editor-timing.txt`.

The successful headset run is at 12:16–12:17 UTC. An earlier 12:14 Home attempt remained at
`no-running-xr-input-subsystem`; do not combine those attempts into one timing measurement.

| Interval | Initial Home | Ocean Villa |
| --- | --- | --- |
| XR readiness wait | 1,193.2 ms, frames 319–350 | 2,058.7 ms, frames 1355–1373 |
| Avatar build request to ready | 24,595.3 ms, 12:16:05.398–12:16:29.992 UTC | 603.0 ms, 12:17:09.665–12:17:10.268 UTC |
| DCA initialization event to build-character-begun | 15,785.6 ms, both on frame 350 | About 1.3 ms, frame 1373 |
| Recipe updated to character generation begun | 7,643.8 ms, frames 350–351 | 25.4 ms, frames 1373–1374 |

The same-frame 15.8-second startup interval confirms a stalled application frame, consistent
with the reported hourglass. Continued headset movement does not establish that application
frames were advancing. The additional 7.6-second interval is not attributed by these markers;
do not call all of it mesh generation or attribute it entirely to UMA without a fresh profile.
The diagnostic `unattributedWaitOrPreprocessMs` is a coarse residual, not a CPU profiler sample.

Earlier September 4 profiling already established an 18.46-second synchronous AssetIndexer
and referenced-asset load under `DynamicCharacterAvatar.Start`, before the September 27 Home
lifecycle extraction. See `Logs/MetaOpenXR-Fix-2026-09-04/UMA-PROFILE-FINDINGS.md` and the raw
capture/report in `Library/GHAProfiles`. Its full frame was 25.29 seconds and included a separate
editor wait. This is historical evidence of the failure class, not a profile of today's run.
First-use asset loading is the leading explanation for slow Home followed by fast world builds.
The September 27 Desktop result was not a controlled cold-start comparison, so VR-specific
causation and absence of a Desktop cold-load stall remain unproven. Do not dismiss the owner's
regression report or claim the recent changes have been exonerated by this comparison alone.

Both avatars reached two renderers, a ready Animator, arm IK and locked calibration using the
saved 1.630 m height. The owner-observed fast world appearance adds successful evidence for that
case; it does not close A5's seated-start/tracking/visibility matrix. The earlier 28.65-second
post-build calibration wait is a distinct failure. Eight pointer errors recur around world spawn
and authority setup; A4 remains open independently of the initial-load stall.

Next focused verification: capture the initial load's raw CPU hierarchy in a fresh VR run and
a comparable cold Desktop run, including the 7.6-second interval, and identify the specific
loaded assets/editor waits. Reuse the existing explicit profiler tooling; avoid broad repeated
feature tests. If the index-loading cause is confirmed, reassess the existing catalog-scoped
index/local asynchronous-loading plan in `Logs/UMA-Streaming/IMPLEMENTATION-PLAN.md` against the
current two-race catalog and installer changes. Its September 4 one-race/76-record audit is not
a current content manifest. No performance fix, vendor modification, staging or commit occurred.

### October 1 owner decision — UMA 3.1 migration at the final release gate

Continue separation and functional work with the currently pinned UMA baseline. The owner
reports UMA 3.1f1 released and a 3.1f2 bug fix under development; these release details have not
been independently verified in this documentation-only update. Select and verify the actual
3.1 patch when migration begins, rather than upgrading now.

Added **P5-4** to Phase 5 and §9 of `GHA-IMPLEMENTATION-PLAN.md`: after separation and a working
current-version baseline, migrate the integration to a reviewed UMA 3.1 patch **before inviting
additional testers or public release**. Preserve the baseline, review API/content/shader changes,
update pins and compatibility records, rebuild the Global Library, verify saved-avatar
compatibility and Desktop/VR/network behavior, then regenerate and revalidate installable
packages and guides. Prior version evidence does not close the upgraded release's gates.
P5-3 publication depends on P5-4. Existing IDs and Phases 0–5 remain; the parent-item count is
now 46 (previously 45). Re-review this gate with the owner when it is reached.

The owner intends to test VR next; no new VR result is implied. No Unity state, UMA installation,
source pin or runtime code changed, and no new staging, commit or push occurred.

### September 27 Phase 1 D1 Home lifecycle slice — PROVISIONAL

The owner directed proceeding with the discussed work. Read the linked static-avatar discussion
at Phase 1 entry: use a prepared, imported Humanoid model with fixed appearance, normal
animation/tracking, stable model identity and explicit first-person head visibility. The full
static-avatar plugin remains later work; no RPM asset was copied or selected. All four required
Notion pages were retried but inaccessible, so this slice uses inspected repository source.

`VertexFormHomeAvatar` now owns Home startup/readiness, Save/apply, Classic construction,
presentation and scene-transition teardown. `IHomeAvatarProvider` supplies the optional build,
instance and teardown seam. `UmaHomeAvatar` retains serialized compatibility fields and public
UI entry points, lazily constructs its UMA puppet, and preserves unknown mode IDs. Existing
combined prefabs obtain the host through a compatibility shim; new host installs wire it without
UMA. UMA installation requires the host Home component first. No scene or prefab was saved.
The full D3 Home/network registry, network/player extraction and remaining host dependencies in
the UMA puppet/UI are still pending; this local one-adapter seam is not D1 or C1 completion.

Final compilation succeeded with no errors. `Tools/ValidateGhaHomeLifecycle.cs` passed eight
Edit Mode checks: queued Classic Save without a UMA adapter, preview/application separation,
Save listener removal, provider build and transition teardown, provider-to-Classic switching,
missing-provider fallback preserving the saved ID, idempotent host wiring on a disposable
prefab copy, and preservation of peer IDs by the installed adapter. Fixtures and preferences
were restored. Evidence: `Logs/Phase1Home-20260927/edit-mode-validation.json` (ignored).
This is fixture validation with UMA still installed, not runtime or true package-absence proof.
Focused Desktop Play Mode approval was requested for the changed path and remains pending.
The Editor was not put into Play Mode.

Code, new metadata, validation script and this evidence update remain unstaged for review.
The previously approved 11-file documentation index is retained. No commit or push occurred;
BUG-3, A5.1 and A5.2 remain deferred.

#### September 27 owner-started run: avatar blocked on XR initialization

The owner manually started the app and reported no avatar and no Console errors.
Read-only live inspection found HomeScene configured as VR, XR startup enabled but initialization
incomplete, no active loader/input/display subsystem and no valid tracked headset. The host had
found the UMA adapter and Classic root; startup was still waiting on the existing XR readiness
gate, before any UMA puppet/build. Console sequence 930 warns that initialization produced no
active loader; 949/951 show the Home wait and `no-running-xr-input-subsystem`.
The same VR startup wait exists in the pre-extraction `UmaHomeAvatar` at HEAD.
This identifies the immediate blocker, not a successful Phase 1 runtime check or a conclusion
about the underlying headset/runtime failure. Clarification of Desktop versus PC Link testing
was requested. No runtime settings, source, assets or Play Mode state were changed.
Read-only evidence: `Logs/Phase1Home-20260927/missing-avatar-live.json` (ignored).

#### September 27 headset-connected run: owner reports working; first-load delay recorded

The owner reconnected/put on the headset and reported that everything looked good except a slow
first load. This supersedes the earlier uncertainty about the intended test mode: this was PC
Link. It is owner-observed Home behavior, not acceptance of every Phase 1/network/Save case.
Read-only timing evidence from the new run:

| Step | Evidence |
|---|---|
| Home XR readiness | 13:38:39.628–13:38:40.704 UTC; 1,076.3 ms, sequences 993–1006 |
| Initial UMA avatar | 13:38:40.713–13:38:56.732 UTC; 16,019.4 ms, success, two renderers and Animator ready |
| Largest measured gap | DCA-start initialization to build-character-begun: about 14.35 seconds, both on frame 153; the trace places the stall before that build event but does not attribute it to a particular operation |
| Later customization preview | 396.9 ms, success, sequences 1046–1048; a different build path, not a controlled warm/cold comparison |

VR arm IK became ready after character creation (sequence 1032). The captured trace does not
include a completed height-calibration marker; do not convert the owner's general report into
specific calibration or tracking-recovery acceptance. A `PhotonCloudTimeout` was also logged at
sequence 1024, after the long frame; its causal relationship to the stall is unproven.
Keep the slow first load under existing E3 / R9 / BUG-9; no performance fix, runtime setting
change, restart, commit or push was performed. Evidence:
`Logs/Phase1Home-20260927/headset-return-timing.json` (ignored, filtered timing/error messages).

#### September 27 Ocean Villa transition — FAILED route acceptance; diagnosis in progress

The owner joined Ocean Villa and reported ten Console errors plus delayed first-person body
visibility. The prior statement that no further checks were needed was too broad: the eight Edit
Mode fixtures and observed Home behavior did not verify the Login/Home/world/return route.
Keep A4/A5 and D1 runtime acceptance open. Do not label these failures harmless or cleared.

The ten errors in this Play session are two earlier Photon authentication/lobby timeout errors
(1023–1024, 13:38:55 UTC), plus eight `No available indices for pointer registration.` errors:
four during Fusion player instantiation (1251–1254) and four in
`XRRigController.ApplyMultiplayerPlatformAndAuthority` (1294–1297). These are separate from old,
already-corrected compile errors retained in Pipeline's historical Console buffer.

Read-only inspection confirms that RoomManager actually spawns
`NewGenericMRDesktopPrefab.prefab` for this VR session. It has 12 authored-active, UI-enabled
hand/controller NearFar/Poke interactors, while installed XRI's `XRUIToolkitHandler` has eight
slots. Its modality manager starts disabled. `XRRigController` then activates the hand roots in
`VRObjects` before enabling the modality manager. The two activation points match the two
four-error bursts. After settling, six live controller pointers are registered; the hand pointers
and temporary rig are inactive, and there are no destroyed registry entries in the captured state.
No UI Toolkit documents were loaded. Do not patch vendor/package-cache code or simply raise the
pointer limit: correct ownership/activation of local hand/controller groups and remote rigs.

World avatar construction itself finished in 523.9 ms at 13:40:10.409 UTC, following 2.082 s of
XR readiness. Both renderers remained on the hidden layer until height calibration locked at
13:40:39.061 (eye height 1.630 m, scale 0.889): an additional 28.65 s hidden after construction.
This is a readiness/visibility defect to investigate under A5, separate from E3's first Home
load stall. Preserve the documented seated-start behavior (authored/locked scale until valid
standing calibration), and verify saved calibration reuse and Home/world handoff.

The network bridge/puppet, calibration/alignment code, RoomManager and XRRigController have no
uncommitted source changes relative to HEAD. The used prefab's existing diff only removes four
null AR references. This narrows direct changes but does not prove absence of a timing regression;
a controlled comparison and full route verification remain outstanding.

Evidence is under ignored `Logs/Phase1Home-20260927/`: `scene-transition-errors.json`,
`scene-transition-timeline.json`, `world-visibility-timing.json`, `world-pointer-state.json`
and `active-player-prefab-pointer-setup.json`. The earlier `vr-prefab-pointer-setup.json`
inspects the similarly named VR prefab, not the one used by this run.
No runtime code/settings or Play Mode changes were made during diagnosis. Permission to stop
and restart Play Mode for correcting/verifying these failures was requested under AGENTS.md;
it was subsequently granted with a preference for Desktop testing, as recorded below.
No new staging, commit or push occurred during diagnosis.

#### September 27 owner-authorized Desktop switch and restart

The owner explicitly authorized stopping/restarting as required and requested Desktop testing
because the headset is needed elsewhere. Stop/start permission remains available for this
testing work; do not ask again for the same authorized scope. Desktop is the current test mode.
The VR pointer/calibration defects remain open and require later headset validation.

Stopped the VR session using Unity CLI. Through Editor APIs, saved Platforms.platformChoice as
Desktop and disabled Initialize XR on Startup for Standalone only
(`Assets/XR/XRGeneralSettingsPerBuildTarget.asset`); Android settings remain unchanged.
`SceneLoader` previously requested XR startup unconditionally at login and world transition.
A narrow source change now skips both requests for Desktop-style presentation, so a Desktop
session does not acquire the headset. This core change is separately reviewable and unstaged.
Compilation succeeded with no errors. All four required Notion pages were retried but inaccessible.

Restarted from LoginScene, used the normal login action with the existing saved name and verified
HomeScene in Desktop mode: host and UMA avatar ready, two renderers, Animator ready, no XR loader,
input/display subsystem or VR readiness wait. Avatar build was 3,086.6 ms; no new captured errors
since console cursor 1519 through this Home check. This verifies Desktop startup/Home only;
world transition, return Home and affected Save checks are still pending. The app is left running
in Desktop Home for the owner. Evidence: `desktop-home-state.json` and
`desktop-home-console.json` under ignored `Logs/Phase1Home-20260927/`.

Headset review: the project's Meta Quest target list includes Quest 2, Quest 3 and Quest 3S.
No Quest-3-only requirement was found in the avatar integration. Quest 2 is supported by
[Meta Link](https://developers.meta.com/horizon/documentation/unity/unity-link/) and is a suitable
candidate for later PC Link checks; this is not device-specific acceptance of our app or its
standalone performance. Optional Android Meta Quest Occlusion is enabled with required=false;
its environment-depth extension targets Quest 3 and later according to
[Meta's OpenXR feature table](https://github.com/meta-quest/Meta-OpenXR-SDK).
That optional mixed-reality feature is outside ordinary Desktop/VR avatar testing.
No vendor changes, package changes, new staging, commits or pushes were made.

#### September 27 Desktop world observations — functional behavior good; pointer errors remain

The owner reports everything working with no noticeable delays in Desktop, except the pointer
registration errors. Read-only inspection confirms Ocean Villa is active in Desktop mode, with
XR startup disabled, no active loader and no input/display subsystems. This verifies that the
Desktop world transition did not initialize XR. Return Home was not separately confirmed.

Console inspection since the Desktop run's cursor 1519 returned exactly four errors through
cursor 1928: `No available indices for pointer registration.`, sequences 1780–1783 at
14:49:09.084–.086 UTC. All four stacks originate in Fusion player prefab instantiation through
`RoomManager.SpawnVRPlayer`. This matches the previously inspected 12 authored-active
interactors competing for eight slots before platform/authority setup runs in `Start`.
The pointer defect is therefore shared by Desktop and VR; A4 remains open despite otherwise
successful owner-observed Desktop behavior. No runtime fix was made during this inspection.

The earlier VR first-load stall and delayed body visibility remain unresolved under E3/A5.
Desktop success does not establish VR timing or readiness acceptance; headset retesting remains
later work. All four required Notion documentation pages were retried and remain inaccessible.
Only this runbook and the implementation plan were updated; Play Mode was left running, and
no new staging, commit or push occurred.

### September 26 prior owner checks accepted; duplicate testing withdrawn

The owner confirmed avatar editing and saving were already tested successfully when implemented
and directed proceeding without repeating those checks. Use this as prior owner baseline
verification, alongside the recorded Desktop tests and September 24 PC Link observations.
No fresh edit/Save run is required for the unchanged baseline; repeat affected checks only after
relevant implementation changes or a newly observed failure. This supersedes the September 24
request for another edit/Save pass, without inventing results for distinct unreported checks such
as controlled tracking recovery or VR-to-Desktop arm handoff. A5.1/A5.2 remain deferred.
Documentation review, separate local-commit approval and the next-phase scope review remain in
effect. No runtime changes, commits or pushes were made as part of this decision.

### September 24 PC Link observations and deferred comfort fixes

The owner entered Play Mode manually with the headset connected. Initial UMA Home observation
found about a 12-second gap between avatar construction/mirror visibility and first-person body
visibility. Console evidence identifies height-calibration readiness as the remaining gate; the
owner then confirmed the body was properly visible. This is partial A5 evidence, not full PC Link
acceptance. See the [dated baseline findings](GHA-SEPARATION-BASELINE-2026-09-23.md#september-24-pc-link-observations--partial-a5-evidence).

The owner explicitly requested two later fixes, now tracked in the implementation plan:
A5.1 for arms intersecting the torso, and A5.2 for a viewpoint that feels too far back inside the
body, exposing too much upper chest when looking down. Assess arm fit/IK and avatar-eye alignment
later; no fix or camera offset change was made. The owner reported the remaining observed
behavior seemed okay. Save/rebuild, controlled tracking recovery, return-to-Desktop arms and
restoration still need explicit results. The initial calibration delay remains an open finding;
BUG-3 walking retains its separate deferral. No commit or push is authorized by these observations.

### September 23 Phase 0 inventory and Desktop baseline

The owner authorized proceeding with the current plan, explicitly approved Desktop Play Mode
checks, and prohibited pushes; any approved commits remain local. Exact inventory, reviewed
scope and evidence: [separation baseline](GHA-SEPARATION-BASELINE-2026-09-23.md).

Read-only upstream verification found official Master still at `58bf4e775653c8befbc6d36584742360dbc7033a`.
GHA remains at `8c19b5f7`; no merge, package upgrade or Git mutation was performed. Classified
101 existing working-tree entries and 232 committed paths relative to upstream. The installed
Pipeline record is `0.6.0-exp.1`; the older 0.5.0 record below remains historical. All four required
Notion pages were inaccessible again.

Three approved Desktop sessions from LoginScene exercised Classic preview/Next/Save, Male to
Female to Male, saved height DNA, repeated Save/rebuild visibility, close/reopen, and separate
UMA/Classic restart persistence. Both modes reconstructed the saved Home body. UMA height 0.65
encoded to 166/255 and reconstructed as 0.6509804. Rebuild renderers retained hidden layer 7.
There were zero new captured Console errors (cursor 258 through 493); compile status was clean
and the final recompile request was up to date. These were one-shot Button/Slider event checks,
not mouse-input, walking, PC Link, network or UMA-absence acceptance.

Original avatar/login preferences and temporary in-memory XR startup changes were restored and
verified. Editor returned to stopped, clean LoginScene. All 98 hashed pre-existing nonsensitive
working-tree files were unchanged after testing, before this evidence update. No implementation,
asset save, vendor modification, staging, commit or push occurred. The baseline report proposes
an exact 11-file documentation-only local checkpoint for owner approval.

Subsequent owner instruction: stage only the proposed 11 documentation files for review; do not
commit until the owner reviews and separately approves. BUG-3 walking investigation is deferred
until later because the owner, despite observing it and receiving another person's report, cannot
reliably reproduce it. The defect remains unresolved; capture evidence if it recurs. General
locomotion acceptance is not closed by this deferral.

Owner phase-by-phase plan review is now recorded: Phases 0–3 and Phase 5 approved; Phase 4
conditionally accepted. Re-review each upcoming phase when its predecessor is complete, before
starting that phase. The current plan has six phases numbered 0–5; later/deferred work remains
in its register. The numbered migration phases farther below are historical and are not an
additional Phase 6 for the current separation plan.

Phase 0 remains open for PC Link evidence and documentation/inventory review plus separate local
commit approval. Desktop baseline P0-3 is complete within its recorded scope. Read-only readiness
recheck found the exact GHA Editor ready, compilation up to date with no errors, and no newly
captured errors since cursor 493 (current cursor 494). No new Play Mode session was run.
At that September 23 checkpoint, Phase 1 runtime extraction had not begun; the September 27
entry above records the first source slice. The broader runbook matrix and all remaining
roadmap/defect items stay tracked.

For D2, the owner defines static avatars as fixed in appearance, with animation/tracking and
other supported embodiment behavior retained. At the Phase 1 entry review, read the
[related discussion](codex://threads/01a0c97b-c915-7b41-8020-2c62d65371d2) and assess the local RPM
model candidates recorded in the implementation plan. Their files were located in the retired
RPM reference project; Unity/GHA compatibility is not yet accepted. Retain reusable proof code
for the later full static-avatar package without making that product a prerequisite to UMA.

Phase 3 exports/tests the GHA Host and GHA UMA Integration packages; UMA is acquired separately.
An explicitly approved integration release may use the verified temporary host package before
upstream acceptance. All commits remain local under the owner's current instruction; approval
of plan content does not authorize a commit, push or publication.

### September 23 separation plan and installation-source policy

The [implementation plan](GHA-IMPLEMENTATION-PLAN.md) now sequences the next work. This runbook
remains authoritative for ownership, evidence and acceptance status. Earlier migration phases and
dated findings remain historical evidence; they do not require replaying commits or executing an
older packaging sequence. No runtime gate is closed by this documentation revision.

- Finish host/provider separation in the combined project before splitting working copies.
  Shared Home/player lifecycle and Classic Save must move out of UMA; runtime provider
  registration, host contracts and a minimal non-UMA Humanoid proof are prerequisites.
- Keep the generic host contribution in this VertexForm fork. Maintain UMA integration code,
  export tooling and developer scripts in a separate VertexForm working copy. Another branch
  in this fork is sufficient; final repository/branch names and visibility remain owner choices.
  No separate standalone GHA UMA repository is required. A custom UMA fork is only for UMA changes.
- Preserve the combined baseline and development history. Use ordinary refactoring/removal
  commits; no required cherry-picking, replay, rebase or force push.
- Prefer UMA from the Unity Asset Store, then an official GitHub release package, then a local
  source checkout. Preserve `Tools/Sync-Uma.ps1` and the developer route. Package users must not
  require a UMA checkout. These are explicit alternatives, never automatic fallbacks.
- Pin and validate the actual distribution artifact, supported host and integration versions.
  Asset Store and release-package acceptance remains pending; the source pin and its shader
  repairs do not prove those artifacts work. One installed UMA distribution per project.
- Keep all remaining architecture R1–R15 and BUG-1–BUG-12 work tracked. The plan separates
  pre-split blockers from later product work; release claims require the applicable platform,
  network and installation gates. It does not authorize dropping existing migration requirements.

Read-only inspection found HEAD and the locally tracked GHA remote reference at
`8c19b5f773985b71045df92337fc107595512d56`, including `26ae0783` and `8a8c86ff`.
This is local reference evidence, not a fresh remote publication check. The September 18
"not yet pushed" statement below describes that earlier checkpoint.

This revision edits documentation only. No runtime code, package version, scene, prefab, branch,
staging area or publication was changed. Existing owner edits, including removal of
`com.unity.collab-proxy` and local platform/serialization changes noted below, remain untouched.
Before implementation, inventory those changes and obtain scoped checkpoint approval; never
include credentials, ignored UMA vendor content, generated state or unrelated work.

### September 18 documentation split, packaging decision and Home fixes

Documentation: `GHA-GETTING-STARTED.md` was renamed `GHA-developer-getting-started.md` (source-clone
workflow for contributors) and a draft `GHA-getting-started.md` describes the intended package-based
user installation. `Packages/com.vertexform3d.gha/Documentation~/ARCHITECTURE.md` was brought up to
date (retitled for GHA; current-implementation inventory; July–September status history; roadmap
R1–R15 for every undelivered design commitment; known-defect register BUG-1–BUG-12).
At that date, `GHA-IMPLEMENTATION-PLAN.md` sequenced that work in five lettered phases awaiting
owner review. The September 23 numbered phases and review decisions above supersede that sequence.

Packaging decision (owner, September 18): developers build from source with both upstream
repositories; the GHA host layer is to be contributed upstream to VertexForm3D; `.unitypackage`
files are strictly end-user deliverables built from this repository; both package folders move under
`Assets/VertexForm3D/3rdPartyAssets/GHA` before the upstream PR; UPM stays a later option. Nothing
of that is implemented yet; see the plan's Phase B.

Fixes, all `PROVISIONAL` pending commit, VR and second-client passes (owner re-ran each repro on
desktop in the Editor and confirmed resolution):

- BUG-2 (slider DNA never applied to the player or after restart): UMA's `ApplyPredefinedDNA`
  returns early for `useNewDNA` races, which both humans are (read from the RaceData assets through
  the Editor). `UmaAvatarPuppet` and `UmaAvatarCustomizer` now also write DNA through the DCA
  setters before a plain rebuild or after a first/race-change build.
- BUG-1 (two avatars after picking Classic on a session that started as UMA): the Studio defaulted
  to its first tab when no mode was saved while the hosts defaulted to the catalog default, and the
  stock provider switched `SuppressLegacyAvatars` off to build its preview, which also built the
  Classic body onto the rig. `AvatarSelectionManager.ActivateAvatarModelAt` now refreshes only the
  platform preview under suppression (core seam; `VertexFormGhaHost.patch` regenerated for that file
  only and verified to apply to stock `origin/Master` and reproduce the working tree byte for byte);
  the stock provider no longer toggles suppression; `AvatarProviderSelection.DefaultMode` carries the
  host default. Found while regenerating: the working tree's `SceneLoader.cs` carries a world-scene
  resolve-timeout change that is not in the shipped patch; left as shipped, owner decision pending.
- BUG-11 (avatar drawn around the first-person camera after any Save): the puppet handled build
  completion only in `CharacterCreated`, which UMA fires once per character; rebuilds raise only
  `CharacterUpdated`. Completion handling now runs once per non-cancelled build. Evidence: the
  post-fix session logs `phase=UMA_CHARACTER_UPDATED` then `FINISH` (236.6 ms) for the rebuild
  trace `avatar-0004`, where earlier sessions stopped at `UMA_CHARACTER_BEGUN`.

Compilation passed with zero errors after each change (Unity CLI `recompile_status`). Play Mode was
driven by the owner; the agent performed read-only Console and scene inspection only. Committed with
owner approval as `26ae0783` (docs), `8a8c86ff` (gha-uma fixes) and `8c19b5f7` (gha fix); not pushed.

Owner-directed project change, uncommitted: `com.unity.collab-proxy` (Unity Version Control) was
removed from `Packages/manifest.json`. Reason: it fixes or works around a bug where the Android SDK
ends up reported as version 0. Keep it removed; do not restore it when reconciling the manifest or
lock with upstream.

Serialization caveat found September 18: `NewGenericMRDesktopPrefab.prefab` lost the four
`MixedRealityHandler` fields `arCameraManager`, `cameraBackground`, `ARsession`, `arPlaneManager`
when the Editor re-saved it at 10:48 that day. Those fields are declared under `#if !UNITY_WEBGL`,
so with the WebGL build target active the compiled script has no such fields and Unity drops them on
save. All four were null references, the VR prefab (last saved September 7) still has them, and a
save under any non-WebGL target restores them. Harmless to the prefab's behavior, but do not commit it
as GHA work, and run the GHA installers (which save both player prefabs) under a non-WebGL build
target to avoid the same churn on fresh installs.

### September 7 UMA installer catalog-reference recovery

The owner completed the test provider install at 18:51 EDT: 488 default index entries, successful
compilation and no new errors. Subsequent Home runtime testing nevertheless failed the generic
Humanoid guard. Inspection confirmed both human race definitions were valid, but the installed
Home/player/panel catalog fields and panel slider had been saved as null. The earlier log/hash
check established that files were saved, not that their required references were correct.

UMA's `RebuildLibrary` starts asynchronous `Resources.UnloadUnusedAssets`; Unity does not retain
assets referenced only from stack variables. This is consistent with the first-install reference
loss. The provider installer now loads and statically roots its catalog/slider/controllers before
index creation, releases those temporary roots in finally, validates default/all catalog Humanoid
definitions, checks prefab-save results and verifies saved catalog/controller/slider references
before marking the provider installed. Failed attempts clear installed status without replacing
original snapshots. Runtime errors now distinguish an unassigned catalog from an invalid race.
No runtime substitution, saved-choice reset, vendor edits or catalog ID/default changes were made.

`Tools/ValidateUmaInstallerReferences.cs` passed seven Edit Mode checks in `E:/Test/GHA-Review`:
retention through actual unused-asset cleanup; male default plus two complete human definitions;
invalid default rejection; successful reference repair; byte-identical repeated install;
post-install cleanup retention; and unchanged catalog, existing index and saved recipe. The
corrected provider installer has already been rerun in the review project. Runtime retest is
pending; Play Mode was not entered for this repair. This is not a new full fresh-clone acceptance.

### September 7 clean-test host installer recovery

Owner authorized removing the confirmed obsolete Cesium components and correcting the installer.
Upstream commit `033e4cc4` intentionally removed Cesium but left CesiumGlobeAnchor script GUID
`74f14e1eb550b9a4fb6c0a2f0456845b` on both player roots. Both development and `E:/Test/GHA-Review`
player prefabs were repaired through Unity: exactly 39 lines removed per prefab, consisting only
of that component record and its added-component attachment. Zero missing scripts remain; other
component records and prefab metadata are preserved. No Cesium package was reinstalled.

`GhaVertexFormAssetInstaller` now preflights its target prefabs and Home station before asset
edits, checks every prefab-save result, skips unchanged player component saves, clears the
installed flag on failure, and preserves the original recovery snapshots across retries.
`GhaIntegrationBootstrap` clears failed pending operations and tells the owner to resolve the
error and explicitly rerun the same menu, rather than retrying unexpectedly on domain reload.

Unity 6000.3.11f1 compilation passed in both editors. `Tools/ValidateGhaHostInstaller.cs` passed
eight checks in the test project: valid targets, missing-path rejection, full-install missing-script
preflight rejection without prefab mutation, failed status/original snapshot preservation, failed
Unity save propagation, successful repaired install, byte-identical second install, and one sync
plus one UMA bridge per player with zero missing scripts. The deliberate save-failure case logs
an expected error for a disposable `GhaHostInstallerValidation-*` prefab; temporary assets were
removed. Play Mode was not entered. Host installation is complete in the current test copy;
the owner's next menu is Install UMA Provider Layer, not another host install or uninstall.

Historical uninstall snapshots in both local projects also contained the obsolete component.
Preserved their originals under `Logs/ReviewTransfer/OriginalInstallBackups-20260907` in development,
then removed only those same component/attachment records from the dormant `.backup` files.
Unity reserialization of temporary copies added newer schema fields, so those results were rejected;
the dormant backups were instead patched explicitly and compared to the exact expected deletion.
Active prefab edits were all performed through Unity. Original active prefabs are retained under
each project's `Logs/ReviewTransfer/CesiumRepair-*`. No source VertexForm/UMA repo changes,
staging, commits or publication. Separate malformed UMA graph, inherited environment keyboard
references and sample-model import assertions remain unresolved; full clean-install acceptance
still requires a later fresh-clone test after the remaining issues are addressed.

### September 7 external-review setup guide draft

`GHA-GETTING-STARTED.md` is the reviewer entry point, linked from the root README and UMA
package documentation. (Renamed to `GHA-developer-getting-started.md` on September 18, 2026;
a separate draft `GHA-getting-started.md` now describes the intended package-based user
installation, which is not yet implemented.) It covers public source revisions, Windows sync, integration menus,
local configuration, Desktop/PC Link/two-client checks and known limitations. It is a draft,
not a verified clean-machine install or approval to publish the current dirty worktree.

The initially identified fresh-index gap is now implemented in `GhaUmaAssetInstaller`:
when neither destination index exists, create and populate a project-owned index from the
standard installed `Assets/UMA/` content using UMA's rebuild API. Validate both human races
and slot/overlay/wardrobe entries before persisting it. Existing indexes are preserved;
wrong-type/unloadable occupied paths stop installation without replacement. Do not copy
the source-generated index. The new generated index and its metadata are ignored by Git.

Owner clarified that first-time reviewers should use only the default UMA assets and should
not have to manually rebuild a library. The guide now uses the automatic installer path,
contains no Ready Player Me reference, and identifies Robocopy as included with Windows.

Validation: Unity 6000.3.11f1 compilation passed. Isolated Edit Mode checks in
`Tools/ValidateUmaIndexInitialization.cs` created 488 default-UMA-only entries including both
human races, retained after asset unload/reload; preserved project/installation indexes and project-first precedence; rejected
wrong-type occupied paths; and preserved the development project's actual index. Temporary
test assets were removed. The initial CLI request exceeded Pipeline's 5-second response limit;
the queued test subsequently completed and its SessionState report was retrieved successfully.
No Play Mode test, complete installer rerun, clean-checkout acceptance, staging, commit or
publication was performed. The owner will run the fresh-repository test after including the
reviewed changes; cloning the currently published branch alone does not include this fix.
Pre-publication testing does not require a local commit. At the owner's request, supply the
intended uncommitted setup/UI files directly to the clean test clone, with a SHA-256 manifest
outside that repository; exclude credentials, installed vendor content, generated caches and
unrelated scene/project-setting edits. The base commit plus that manifest identifies the test
snapshot. Keep subsequent test fixes reviewable before committing or publishing.

The test-clone installer dry run exposed an additional first-install preflight defect:
`git check-ignore` did not match directory-only ignore rules when the vendor folders did not
exist yet. `Sync-Uma.ps1` now checks `Assets/UMA/` and `Assets/SourceShaders/` explicitly as
directories, retaining the separate metadata checks and the requirement that vendor content
be ignored. No placeholder vendor folders or bypasses are needed.

### September 7 correction: keep the source UMA checkout verbatim

Owner clarified that approval to use upstream develop shader files was not authorization to
commit a backport on the source UMA master. The earlier approval characterization below was
incorrect. The intended change was to GHA's installed UMA only.

Removed agent-created commit `0c69fee9b` from the source master using `git reset --keep`
after verifying exact HEAD, master branch, upstream parent and a clean working tree.
`E:/src/Unity/6000.3/UMA` now matches official upstream commit `c9204fe4`, with clean
index/working tree and 0 ahead / 0 behind its recorded origin/master. No revert commit, new
branch, push, fetch, ignored-file cleanup, or source configuration change was made. Git's normal
reflog still permits recovery; it does not add a commit to master.

GHA's existing 12 repaired shader files are retained unchanged. `Tools/Sync-Uma.ps1` again pins
official master `c9204fe4`, and for that baseline only exports exact shader blobs from official
develop `f4edf41ba` into destination staging. It never changes source files/index/branch or
requires `0c69fee9b`; no source `.gitattributes` alteration is needed. Missing upstream blobs
stop the import. Review/remove this baseline-specific overlay when advancing the master pin.
No full UMA sync or Unity Play Mode was run for this correction. GHA changes remain unstaged.
Verification: PowerShell parsing and `Sync-Uma.ps1 -ReplaceExisting -WhatIf` passed. Executed
the script's actual shader-export block against isolated staging under
`Logs/UpstreamReview/ShaderSyncValidation-35946e6d9ee54f6e9bdb5357393544b9`: all 12 exports
matched upstream Git blobs and GHA's installed SHA-256 values. Before/after hashes confirm all
12 GHA shaders were unchanged by restoring the source. Source master remained clean and 0/0
against recorded origin/master after validation. No new commits or pushes were made.

### September 7 human body-type selector (runtime acceptance pending)

`PROVISIONAL`: the Body tab now exposes Human Male / Human Female through the existing
selector. `UmaAvatarCatalog.asset` retains male race ID 0 and wardrobe IDs 0-16 unchanged;
female is appended at race ID 1, with seven female wardrobe entries at IDs 17-23. No vendor
content was modified. All clients must ship the same expanded catalog.

The catalog now owns explicit body-type labels and starter outfits. Outfit choices are filtered
by authored UMA race/cross-compatibility and wardrobe slots; missing compatibility metadata is
not treated as permission to equip. Shared hairstyles remain available to both types. Switching
retains compatible choices and restores each type's selections (including None) within the open
customizer session. DNA/color selections remain unchanged; Save Avatar persists the active
type through the existing wire recipe. New types must provide Humanoid RaceData, T-pose and
base recipe; that definition gate is not a substitute for generated-rig/VR acceptance.

Preview and puppet race changes stage wardrobe/DNA/colors while UMA building is disabled,
then enable one build. UMA's internal race-state cache is disabled on these GHA-owned avatars
because the GHA recipe owns their state. Desktop/VR arm policy is otherwise unchanged.

Verification: compilation completed with zero errors. `Tools/ValidateUmaHumanBodyTypes.cs`
passed isolated Edit Mode checks for stable IDs, compatibility rejection, body-type clicks and
wraparound, remembered outfits/None, shape/colors, wire encode/decode, all 17 exposed DNA names
on both races, and selector dimensions. The actual Body controls were rendered and inspected at
`Logs/AvatarStudio-Header-2026-09-07/body-type-verified.png`. A screenshot-cleanup error in the
first capture was corrected; the validation was rerun. `Tools/PlanUmaStreaming.cs` found zero
missing named dependencies, report `Logs/UMA-Streaming/content-plan-20260907-162859.json`.

Play Mode was not entered for this change; permission has been requested. Outstanding: actual
male/female build and back-switch, saving/reopening, Home/player reconstruction, animation,
head hiding and arm IK in VR, remote reconstruction, and device performance. The earlier compact
header and outfit-subsection highlight changes remain uncommitted alongside this work. Nothing
has been staged, committed or pushed for these September 7 UI changes.

### September 5 checkpoint and desktop-arm acceptance

The owner approved completing the staged VertexForm 1.1.9 merge and checkpointing
the six reviewed UMA follow-up files. Merge commit `be13ec6e` records the exact
13-file merge scope, with parents `18bdff38` and `58bf4e77`.

The UMA follow-up includes the reviewed sync pin and setup documentation, UMAKeepChain
and UMAIgnore tags, removal of delayed embedded-preview repositioning, and VR-only
arm IK creation/update. Leaving VR now releases existing arm constraints to the
Animator. Unity compilation completed without errors, and the owner explicitly
confirmed that desktop arm animations are fixed. The owner will recheck VR after
this checkpoint; VR regression acceptance remains PENDING.

Jacket thumbnail/material differences and lighting adjustments are explicitly
DEFERRED at the owner's direction. No material or lighting changes accompany the
arm fix. Sensitive Photon configuration, unrelated local scene/settings changes,
untracked authoring tools and generated files remain excluded and preserved.
No push is included in this checkpoint approval.

### September 5 UMA shader backport (source commit undone September 7)

Historical record, superseded by the September 7 correction above: the agent incorrectly
interpreted permission to use the shader fixes as permission to commit in the source repository.
Local master commit `0c69fee9b4871251d520ae2bfd84868b5a1646f3` contains the exact
12 shader graphs from `f4edf41ba1017b4a745fd1ba7286824332652b7d` plus
`UMAProject/.gitattributes` from `800ee5e99da1d7486bd7c7c777b9579f6221a3d5`.
Unrelated develop code, generated data, user settings and layouts were excluded.
At that time UMA was clean on master, one commit ahead of origin/master; no push was performed.
The local commit must not be described as available from the official remote.

`Tools/Sync-Uma.ps1` then pinned the local commit. The owner requested applying
the approved fixes immediately after reporting pink rendering. The exact 12 shader
files were copied from the owner's clean UMA checkout through the stopped GHA editor
in one AssetDatabase editing batch. No other vendor files or metadata were replaced.
All 12 installed graphs match source SHA-256, load as supported shaders, and report
zero ShaderUtil errors/messages. The skin material resolves to UMA3_SkinShader_URP;
the active pipeline is UniversalRenderPipelineAsset (UniversalRP-WebGL.asset).
No new console errors after cursor 153 (352 after import). No Play Mode visual test.
Backup retained outside Unity's temporary directory at
`Logs/UpstreamReview/ShaderBackup-20260905-151858`. The remaining
`SRP/ShaderGraphs/Materials/UMA_SG_Diffuse.shadergraph` is malformed on develop too
and is explicitly excluded from this backport. Detailed scope:
`Logs/UpstreamReview/UMA-SHADER-BACKPORT-REVIEW.md`.

### September 5 UMA master upgrade

Owner direction: use the existing `E:/src/Unity/6000.3/UMA` checkout, master only;
the owner pulled master. Source HEAD and origin/master are both
`c9204fe475b334162617da5ca923afcc6050e01e`, with a clean source working tree.
No source clone, fetch, pull, checkout, or source modification was performed by this upgrade.

`Tools/Sync-Uma.ps1` now pins that revision, requires the master branch, validates
the authoring allowlist during WhatIf, checks for an open destination editor, and
rechecks source cleanliness/revision before replacement. The Photobooth moved to
`SRP/Samples/Scenes`; its seven camera render textures remain under
`UMA3/Scenes/Prefabs/Textures`. Disposable sample scenes in both locations remain
excluded. The source-generated `AssetIndexer.asset` is explicitly excluded.

After the owner closed GHA, replacement completed: 5,511 UMA files and 83 source
shader files. All 5,594 copied files matched the owner's source checkout by SHA-256
before Unity import. Prior installation retained at
`Temp/UMA-Backup-20260905-100838`. C# compilation passed. Destination Global Library
rebuilt to 508 records. Catalog references (one race, 17 wardrobe entries, four
defaults and three palettes) resolve; the existing read-only recipe dependency
audit reports zero missing explicit slot/overlay dependencies.

Upgrade acceptance is blocked by 13 upstream shader graphs failing import with
JSON parse errors. Their source formatting contains blank lines inside JSON objects,
which Unity's MultiJson parser treats as object separators. No vendor repair or
older-file substitution was applied. Owner approval is required before repairing
the source UMA graphs. Evidence and file list:
`Logs/UpstreamReview/UMA-MASTER-IMPORT-2026-09-05.md`. GHA is open, Play Mode stopped;
runtime/platform acceptance remains pending.
The 13 staged VertexForm merge paths are unchanged; this upgrade is unstaged.

### September 5 checkpoint and VertexForm3D 1.1.9 merge review

The five owner-reviewed checkpoint commits are now local: `4f6e3173`, `72c1c3a8`,
`dd53e48f`, `13968802`, and `18bdff38`. The historical uncommitted-state descriptions
below refer to earlier work, not these saved checkpoints. Nothing has been pushed.

Official Master `58bf4e775653c8befbc6d36584742360dbc7033a` (1.1.9) has been fetched
and merged with `--no-commit`. All conflicts are resolved; 13 files are staged for
owner review before the merge commit. The UI database was preserved and saved
through Unity; all 14 world entries retain their Web-support values. A
`FormerlySerializedAs("WebGPU")` attribute accompanies the upstream `Web` rename.
Unity compilation passed; 10 stock avatars, both Studio providers, one embedded
panel, and preview position/scale `(0.9, -0.5, 0)` / `0.4` were verified. No Play
Mode acceptance test was run. UMA remains at its existing pin. Detailed evidence,
diagnostic limitations and exclusions: `Logs/UpstreamReview/VERTEX-1.1.9-VALIDATION.md`.
This status-note edit is not part of the currently staged 13-file merge scope.

### QuantumVertex source

| Item | Current state |
|---|---|
| Branch | `spike/uma-avatars` |
| Last committed HEAD | `994d9aa4cca49b98e333d19aaededd51fdcbb6b4` |
| HEAD description | `checkpoint: freeze functional avatar framework source` |
| Working tree | Avatar checkpoint is committed; unrelated local/project changes remain uncommitted and excluded |
| Frozen migration commit | `994d9aa4cca49b98e333d19aaededd51fdcbb6b4` |
| Immediate task | Treat this commit as read-only and compare target migration slices against it |

The July 12 handoff records `2e5aa974` as an accepted local-VR baseline. That is historical
evidence, not the final migration source. The later committed HEAD and the current uncommitted
working tree must be reconciled and checkpointed.

### GHA target

| Item | Current state |
|---|---|
| Branch | `Generic-Humanoid-Avatars` |
| Upstream-derived HEAD | `5257c76b7ebce05b43b8d43f4186ce7498a50bea` (official PR #55) |
| Working tree | Dirty; contains the partial GHA migration and unrelated local/project changes |
| GHA package | `Packages/com.vertexform3d.gha` |
| UMA adapter package | `Packages/com.vertexform3d.gha.uma` |
| Host-coupled staging | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration` |
| UMA vendor installation | `Assets/UMA` and `Assets/SourceShaders`, ignored |
| UMA sync script | `Tools/Sync-Uma.ps1` |

All 26 QuantumVertex AvatarSuite C# filenames have a counterpart in the target packages or staging
area. A normalized content audit still reports all 26 as different. Some differences are expected
package/assembly/accessibility changes, but parity has not been established. Treat every migrated
class as `PROVISIONAL` until compared against the frozen source commit.

### UMA baseline

| Item | Value |
|---|---|
| Supported release | UMA NextGen 3.0f4 (`v3.04`) |
| Pinned source commit | `722b308aebfe5dee7d0048e24c1c464c00b5fc06` |
| Upstream feature overlay | None; Icon Creator/Sprite Atlas PR #564 is included upstream in the pinned release |
| Source layout | `UMAProject/Assets/UMA` plus `UMAProject/Assets/SourceShaders` |
| Destination | `Assets/UMA` plus `Assets/SourceShaders` |
| Source-code patch delta from old QuantumVertex UMA | None to reapply |
| Local delta | Purpose-based pruning policy, retained current Markdown docs and selected UMA authoring tools, preserved top-level shader sources, and regenerated Global Library |

`Tools/Sync-Uma.ps1` verifies the commit, stages the copy, applies the pruning policy, and preserves
a recoverable backup during replacement. Its retained `UMA_INSTALLED` call is temporarily commented
out because AnkleBreaker's optional UMA bridge targets UMA 2 and does not compile against UMA 3.

### Upstream Icon Creator and Sprite Atlas overlay

Status: `REMOVED -- INCLUDED UPSTREAM`

UMA PR #564 was merged into `develop` as
`0440f163c8c08253da279fa4f3872fd92a4f2e87`. It makes thumbnail output deterministic, makes Icon
Dimensions functional, adds capture supersampling, and adds the optional race-and-region Sprite
Atlas V2 workflow plus tests and documentation.

UMA v3.04 contains PR #564 and its later fixes, tests, and documentation. On 2026-08-18 the sync
pin was advanced to release commit `722b308aebfe5dee7d0048e24c1c464c00b5fc06`; the feature archive
overlay, documentation-index injection, and feature-commit parameter were removed. The installed
Icon Creator files now come directly from the pinned release.

The ignored GHA UMA installation was rebuilt from a clean detached v3.04 worktree at
`E:/src/Unity/6000.3/UMA-v3.04-sync`. The previous installation remains recoverable at
`Temp/UMA-Backup-20260818-095256`. On 2026-08-25 Unity `6000.3.11f1` completed package resolution
and compilation, the Global Library was rebuilt in place, and the GHA UMA catalog validator passed.
Runtime validation remains pending.

The UMA Core suite was requested in EditMode and completed with 216 tests: 212 passed, three failed,
and one skipped. During its post-test assembly refresh, Unity nevertheless logged an internal
test-runner transition (`Entering playmode with assembly reload locked`); no application runtime
gate was performed. The skipped test covers the intentionally absent optional UMA2 package. Two
failures expect sample content deliberately removed by the sync pruning policy (`UMA3/Scenes` and
`UMA3/RandomCharacters`). The remaining release validator failure reports 11 unresolved optional
HDRP/ShaderGraph script GUIDs in a URP target that does not install those packages. These retained
baseline/pruning-policy exceptions did not produce compiler errors and are separate from the GHA
catalog validation. After the owner reopened the project on 2026-08-26, the exact editor was ready,
Play Mode was stopped, and recompile status completed with no errors.

### UMA documentation and shader-source completeness

Status: `ACTIVE`

The initial pruning policy excluded `Assets/UMA/Docs`, but UMA's Documentation Browser searches
that exact directory. The sync now retains the current Markdown documentation and validates
`Docs/!README.md` plus `Docs/UMAMaterial.md`. The archival `UMA3/Documentation` PDF tree remains
excluded.

UMA 3 also keeps shader authoring sources in the sibling `UMAProject/Assets/SourceShaders` tree,
outside `UMAProject/Assets/UMA`. The shipped `.umaShaderPack` assets contain direct GUID references
to those sources, including diffuse-alpha hair shaders. The sync therefore copies
`Assets/SourceShaders` with its original metadata, validates representative diffuse-alpha hair and
opaque surface-shader sources, and requires both the directory and its `.meta` file to remain
ignored.

UMA v3.04 fixed the former shader-folder issue upstream. `UMAPathUtility.ShaderPackagesRelativePath`
now resolves `SRP/ShaderPackages`, `UMASettings` migrates the legacy value, and the Welcome window
uses that shared path logic. The pinned `WelcomeToUMA.cs` text correction was removed from the sync
on 2026-08-18.

### UMA authoring-tool completeness

Status: `ACTIVE`

UMA content must not be classified as disposable solely because it is stored beneath a demo or
sample directory. Apply these categories before pruning a pinned UMA tree:

- runtime-required content;
- authoring, repair, and diagnostic tooling;
- current documentation and shader/source material;
- disposable demonstrations and obsolete examples.

The original sync excluded the complete `UMA3/Scenes` tree. That removed
`U3-Tools-Photobooth.unity` even though the retained `IconCreator` component depends on that scene
for regenerating incorrect wardrobe thumbnails. The Photobooth also depends on its complete lighting
subfolder and the body, chest, face, feet, hands, head, and legs camera render textures beneath
`UMA3/Scenes/Prefabs/Textures`.

`Tools/Sync-Uma.ps1` continues to omit the approximately 196 MB UMA3 sample-scene tree, but restores
the pinned Photobooth dependency set from an explicit allowlist and fails if any required path is
missing. The retained set is approximately 0.6 MB. Whenever the UMA pin changes, re-audit that
allowlist against the Photobooth scene before accepting the new commit.

Icon generation remains an intentional authoring operation rather than an automatic sync step:

1. Open `Assets/UMA/UMA3/Scenes/U3-Tools-Photobooth.unity`.
2. Set the Photobooth avatar to the race being repaired.
3. Enter Play Mode and select **Generate All Icons**.
4. Review the generated PNGs and the modified `UMAWardrobeRecipe.wardrobeRecipeThumbs` entries.

This isolated UMA authoring workflow is the sole exception to the normal requirement to enter
VertexForm3D Play Mode from `LoginScene`. Stop Play Mode and reopen `LoginScene` before any
VertexForm3D runtime validation.

`IconCreator` saves PNGs beneath `Assets/<rootFolder>/<region>/<race>` and updates the matching
race-specific thumbnail entry on each wardrobe recipe. Because `Assets/UMA` is ignored vendor
content, locally regenerated recipe references are not a distributable GHA fix by themselves.
Before publication, either upstream corrected thumbnails to UMA, pin a source commit containing
them, or provide an explicitly reviewed GHA-owned regeneration/override workflow.

### Temporary UMA 3.0.2 player-build compatibility guard

Status: `REMOVED -- FIXED UPSTREAM`

UMA 3.0.2 commit `dbf76d8e815e60accbf3613c9232190b93b65e31` places these editor-only
custom inspectors beneath the all-platform `UMA_Core.asmdef` without `UNITY_EDITOR` guards:

- `Assets/UMA/Core/Physics/Editor/Jiggle/JiggleBreastEditor.cs`;
- `Assets/UMA/Core/Physics/Editor/Jiggle/JiggleButtEditor.cs`.

Unity therefore includes them in the `UMA_Core` player assembly and fails Windows
Addressables/player compilation because `UnityEditor.Editor` and `CustomEditor` are unavailable.
The clean pinned UMA checkout and the installed files were byte-identical. The official UMA
`develop` head `7449f76dd662cb7ebe0e5d68960e15a43f2241b2` was also inspected on 2026-07-27:
it remains affected, has no editor-only assembly definition/reference at this path, and is
divergent from `master` (four commits ahead and three behind).

User-approved resolution on 2026-07-27: wrap both installed files in `#if UNITY_EDITOR` and have
`Tools/Sync-Uma.ps1` reproduce the guards after copying the pristine source. The sync script ties
the workaround to the affected commit and deliberately stops if the UMA pin changes, forcing this
section and the workaround to be reviewed instead of carrying the delta forward silently.

UMA v3.04 wraps both custom inspectors in `#if UNITY_EDITOR`. On 2026-08-18 the sync's
`Add-UnityEditorCompatibilityGuard`, affected-commit pin, guarded-path list, and related staging and
reporting logic were removed. Windows Addressables/player validation remains required after import,
but the installed files now come directly from upstream without this patch.

### Resolved compile blocker

Status: `RESOLVED`

Enabling `UMA_INSTALLED` causes the installed AnkleBreaker package
`com.anklebreaker.unity-mcp@4c03a9601a8c` to compile its UMA bridge. Against UMA 3.0.2, that bridge
currently produces 30 compiler errors in `MCPUMACommands.cs`, including:

- `UMASlotProcessingUtil.SlotBuildResult` versus `SlotDataAsset`;
- removed or changed `SlotDataAsset.material` and `materialName` APIs;
- read-only `slotName`, `overlayName`, and `raceName` properties;
- changed `UMAAssetIndexer.GetAllAssets` overloads.

This was an AnkleBreaker UMA-version compatibility blocker, not evidence that the GHA runtime
adapter had failed. Inspection confirmed that UMA does not consume `UMA_INSTALLED`; UMA creates it
only as a third-party presence indicator. AnkleBreaker consumes it to compile its UMA 2 command
bridge, route registration, and self-test.

User-approved resolution on 2026-07-27: leave `UMA_INSTALLED` undefined, retain the disabled
auto-add code with an explanatory note, and continue using the rest of AnkleBreaker. Do not patch
`Library/PackageCache`. Revisit the define after AnkleBreaker supports UMA 3 or offers a dedicated
UMA integration opt-out. Unity then reported zero compilation errors through AnkleBreaker.

The AnkleBreaker advanced-tool catalog continues to advertise UMA commands while their guarded
routes are absent; invoking `unity_uma_list_global_library` returned
`Unknown API endpoint: uma/list-global-library`. The user approved treating AnkleBreaker's UMA
category as unavailable for the current migration. Continue using AnkleBreaker for non-UMA Unity
work, and do not interpret advertised UMA tool metadata as a callable UMA 3 integration.

### Target dependency record

Recorded 2026-07-27; VertexForm3D baseline advanced 2026-08-25:

| Dependency | Exact target version |
|---|---|
| Unity | `6000.3.11f1 (3000ef702840)` |
| VertexForm3D target | `5257c76b7ebce05b43b8d43f4186ce7498a50bea` (Starter Kit `1.1.7`) |
| UMA | NextGen `3.0f4` (`v3.04`) at `722b308aebfe5dee7d0048e24c1c464c00b5fc06`; Icon Creator PR #564 included upstream |
| AnkleBreaker | package `2.39.4` at `4c03a9601a8c5133bfca1b580b23866a96b0b603` |
| Unity Pipeline | package `0.5.0-exp.1` |
| Photon Fusion | `2.0.9` |
| GHA | embedded `com.vertexform3d.gha` `0.1.0` |
| GHA UMA adapter | embedded `com.vertexform3d.gha.uma` `0.1.0` |
| Animation Rigging | `1.4.1` |
| XR Hands | `1.7.3` |
| XR Interaction Toolkit | `3.3.1` |
| XR Management | `4.5.4` |
| Meta OpenXR | `2.4.0` |
| OpenXR | `1.16.1` |

The local target branch was fast-forwarded from `346d84c3c47dcd0e0a1800ede649d05028ac7228`
to official Starter Kit PR #55 at `5257c76b7ebce05b43b8d43f4186ce7498a50bea` on 2026-08-25.
`origin/Generic-Humanoid-Avatars` remains at the older baseline, so the local branch is four commits
ahead solely because of the fetched upstream history. Migration work remains uncommitted. Nothing
was staged, committed, stashed, or pushed during the integration.

### Upstream Starter Kit PR #55 integration

Status: `DONE`

The clean reference checkout at `E:/src/Unity/6000.3/vertexform3d-unity-vr-starterkit` was on
`Master` at `5257c76b7ebce05b43b8d43f4186ce7498a50bea`. Relative to the former GHA baseline,
the official update changed six files:

- removed the unused `com.cesium.unity` dependency and its lock entry;
- removed Moon Terrain from the local Addressables scene group;
- advanced `Project Data SO.asset` from package version `1.1.6` to `1.1.7`;
- added the Tutorials link to `VertexForm3DHelp`;
- corrected `VertexFormSDKMenu` to search for `SettingsUISO` and reuse the asset at the expected
  path before trying to create it.

Only `Packages/manifest.json` and `Packages/packages-lock.json` overlapped the dirty GHA migration.
The reconciliation accepts the upstream Cesium removal while retaining the GHA-owned AnkleBreaker
dependency and the embedded `com.vertexform3d.gha` and `com.vertexform3d.gha.uma` lock records.
The four non-package files match the clean reference checkout after line-ending normalization.
Unity `6000.3.11f1` subsequently resolved 80 packages, completed compilation, and reached an idle
editor state with no relevant compiler errors. Pipeline `0.5.0-exp.1` was added to the resolved
lock; Cesium remained absent and the retained AnkleBreaker/GHA records remained intact.

### Target dirty-file ownership record

Recorded 2026-07-27:

| Classification | Files or paths | Handling |
|---|---|---|
| GHA migration | `Packages/com.vertexform3d.gha/**`, `Packages/com.vertexform3d.gha.uma/**`, `Assets/VertexForm3D/3rdPartyAssets/GHA/**` | Keep migration-scoped and review by package boundary. |
| Minimal host seams | `AvatarInputConverter.cs`, `AvatarSelectionManager.cs`, `PlayerNetworkSetup.cs`, `SitSpot.cs`, `SceneLoader.cs` | Reconcile against the frozen QuantumVertex commit; keep independently reviewable. |
| Required package/setup wiring | `.gitignore`, `Packages/manifest.json`, `Packages/packages-lock.json`, `Tools/**` | Keep; excludes UMA vendor content, pins tooling, registers embedded packages, and supplies the reproducible UMA/install workflow. |
| UMA-owned project delta | `ProjectSettings/TagManager.asset` | UMA 3.0.2's `Assets/UMA/Core/Editor/Scripts/ImportProcessor.cs` unconditionally ensures `UMAIgnore` and `UMAKeepChain` during asset post-processing. The tags remain while UMA is installed and are not owned or removed by either GHA integration layer. Exclude this file from a GHA-only change set unless the separately reviewed UMA installation policy explicitly includes it. |
| Migration documentation | `AGENTS.md`, `GHA-MIGRATION-RUNBOOK.md`, package documentation, staging `README.md` | Keep with the migration. |
| Credential-bearing local configuration | `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset` | Never stage or commit. |
| Generated/editor state | `Assets/XR/Settings/OpenXR Package Settings.asset`, `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/ShaderGraphSettings.asset`, `ProjectSettings/URPProjectSettings.asset`, `vertexform3d-unity-vr-starterkit-GHA.slnx` | Exclude unless a later gate proves a specific functional setting change is required. Current tracked settings changes are line-ending-only after removing `UMA_INSTALLED`. |
| Unrelated local content | Ocean Villa `diet-healthy-...tint0xF4B81CFF.jpg` and `.meta` | Preserve locally; exclude from migration. |
| Unrelated editor cleanup | deleted `VertexForm3DPackageExporter.cs.meta` | Exclude pending separate cleanup; it is not part of GHA migration behavior. |
| Ignored vendor state | `Assets/UMA/**`, `Assets/UMA.meta` | Never commit; reproduce from the pinned checkout. The local `ImportProcessor.cs` opt-out is a machine-local safeguard. |

## Ownership boundaries

### VertexForm3D core

Core may contain only minimal, inert, provider-neutral seams. Candidate migrated changes include:

- `PlayerNetworkSetup.AvatarConstructionOverride`;
- provider-neutral avatar/posture state access and seating events;
- `AvatarSelectionManager.SuppressLegacyAvatars`;
- generic seated correction support in `AvatarInputConverter`;
- `SitSpot` occupancy and `SeatedPelvisTarget`;
- the generic `SceneLoader` EventSystem sweep/activation retry;
- provider-neutral core seams only; Fusion-specific `AvatarExtensionSync` remains in the
  VertexForm host-integration layer.

Core must not reference UMA, GHA provider implementations, recipes, wardrobe, DNA, or UMA catalogs.

### `com.vertexform3d.gha`

Owns provider-neutral Unity Humanoid behavior:

- contracts and capability surfaces;
- calibration and physical-fit policy;
- rig input abstraction;
- arm IK and future generic leg/finger/face contracts;
- locomotion and posture;
- tracking lifecycle and XR startup stabilization;
- presentation and camera visibility;
- provider-neutral network payload/posture structures;
- generic diagnostics;
- eventually the provider-neutral avatar configuration panel shell.

No file in this package may import UMA.

### `com.vertexform3d.gha.uma`

Owns UMA-specific behavior:

- DCA construction and rebuild lifecycle;
- catalog, recipes, DNA, wardrobe, shared colors, and persistence;
- renderer discovery and head separation;
- UMA customizer controls;
- UMA validation/import setup tooling.

It depends on GHA and a separately installed UMA checkout. It never vendors UMA source.

### Host-coupled integration staging

`Assets/VertexForm3D/3rdPartyAssets/GHA/Integration` temporarily owns classes that still directly
depend on current VertexForm3D implementation types such as `PlayerNetworkSetup`,
`AvatarInputConverter`, `ProjectManager`, `RoomManager`, `SitSpot`, and the legacy Home station.

Files leave staging only after stable host contracts replace those direct dependencies.

### Project wiring

Scenes, player-prefab component wiring, UI layout assets, catalog assets, Animator assets, build
profiles, Photon versioning, and platform settings are project integration. They are neither
generic framework source nor UMA vendor source.

## Ordered migration phases

Do not skip a gate because a later phase appears to compile.

### Phase 0 -- Freeze QuantumVertex

Status: `DONE`

Actions:

1. Stop new timing analysis and performance optimization in QuantumVertex.
2. Preserve the existing timing diagnostics and known delay observations without attempting to
   resolve them.
3. Confirm the current functional source compiles and run only the minimum smoke test needed to
   ensure the checkpoint is coherent.
4. Record known functional limitations separately from deferred performance concerns.
5. Separate unrelated user changes from the avatar checkpoint without deleting or reverting them.
6. Commit the current avatar source, required host hooks, assets, prefabs, and scenes.
7. Record the final commit below and make QuantumVertex read-only for migration.

Required record:

```text
Frozen QuantumVertex commit: 994d9aa4cca49b98e333d19aaededd51fdcbb6b4
Checkpoint date: 2026-07-27
Minimum functional smoke test: Zero-error Unity compile plus project-owner acceptance from the
last known-good functional VR test; a current headset was unavailable and no new performance claim
was made.
Known functional limitations: Recorded in AVATAR-FRAMEWORK-CHECKPOINT-2026-07-27.md at the frozen
commit and carried into the original-plan reconciliation ledger below.
Performance work: Deferred until post-migration Phase 8
```

Gate 0:

- a named, immutable QuantumVertex commit exists;
- the avatar-relevant working tree at that commit is understood;
- known functional limitations and the performance deferral are written down;
- no target parity claim uses an uncommitted source snapshot.

### Phase 1 -- Restore a trustworthy GHA baseline

Status: `DONE`

Actions:

1. Resolve the AnkleBreaker/UMA 3.0.2 compatibility failure.
2. Confirm `UMA_INSTALLED` remains intentionally undefined until AnkleBreaker supports UMA 3 or
   provides a dedicated integration opt-out.
3. Obtain zero GHA/UMA compile errors.
4. Inventory and classify every dirty target file as migration, required local setup, generated
   state, credential-bearing configuration, or unrelated user work.
5. Record exact versions for Unity, VertexForm3D target commit, UMA, AnkleBreaker, Fusion, XR
   packages, and GHA packages.
6. Confirm the target branch still starts from the intended upstream
   `Generic-Humanoid-Avatars` history before creating migration commits.

Gate 1:

- clean compilation;
- no unknown dirty-file ownership;
- credentials and ignored vendor content are excluded;
- dependency versions are recorded.

### Phase 2 -- Reproduce the external UMA installation

Status: `DONE`

Actions:

1. Run `Tools/Sync-Uma.ps1 -WhatIf`.
2. Run the pinned UMA v3.04 installation/replacement only after reviewing the plan.
3. Confirm `Assets/UMA` and `Assets/UMA.meta` remain ignored.
4. Rebuild the UMA Global Library through compatible editor tooling.
5. Verify all races, wardrobe recipes, materials, and palettes referenced by
   `UmaAvatarCatalog.asset`.
6. Record the installed commit and pruning result.

Completion evidence recorded 2026-08-25:

- installed source commit `722b308aebfe5dee7d0048e24c1c464c00b5fc06` with the documented
  purpose-based pruning policy;
- Unity `6000.3.11f1` completed package resolution and compilation;
- the ignored Global Library index was rebuilt rather than copied, yielding 146 slots, 114
  overlays, 11 races, 10 text recipes, 162 wardrobe recipes, 34 UMA materials, and one mesh-hide
  asset;
- the GHA UMA catalog validator passed for one race, 17 wardrobe entries, four default wardrobe
  entries, three color channels, 26 slot references, 28 overlay references, and seven unique
  materials;
- `Assets/UMA` and the rebuilt index remain ignored; no UMA vendor files were staged.

Gate 2:

- reproducible UMA installation from the pinned checkout;
- Global Library rebuilt rather than copied;
- catalog references resolve;
- no UMA vendor files are staged.

### Phase 3 -- Establish file-by-file parity and package placement

Status: `DONE`

For each source file at the frozen QuantumVertex commit:

1. Classify it as core, generic GHA, UMA provider, host staging, project wiring, diagnostic, test,
   or obsolete.
2. Compare source behavior with the target counterpart.
3. Reapply missing behavior intentionally; do not overwrite target package adaptations blindly.
4. Rename remaining QuantumVertex/QV identifiers to GHA where they describe this product.
5. Preserve provider names such as `Uma*` when the class is genuinely UMA-specific.
6. Preserve Unity GUIDs for moved assets where references depend on them.
7. Record the result in the migration ledger.

Special checks:

- generic GHA contains no UMA imports;
- provider-neutral contracts do not depend on the UMA package;
- host adapters do not leak into generic runtime assemblies;
- asmdef references express the intended dependency direction;
- animation controller parameter/state names remain synchronized with runtime code;
- catalog wire IDs remain append-only.

Gate 3:

- every QuantumVertex AvatarSuite source and asset is accounted for;
- no item is omitted without a documented `SUPERSEDED` or `DEFERRED` decision;
- package boundaries compile independently as designed.

### Phase 4 -- Reconcile minimal VertexForm3D core changes

Status: `PROVISIONAL`

Actions:

1. Diff each target core file against the current upstream branch, not against old QuantumVertex
   wholesale.
2. Port only the smallest provider-neutral behavior required by GHA.
3. Keep each core seam inert when GHA and UMA are absent.
4. Add null/bounds guards needed to keep stock avatars functional.
5. Keep `AvatarExtensionSync` in the stable VertexForm host-integration layer; do not add Fusion
   to the provider-neutral GHA runtime assembly.
6. Verify stock-only behavior before enabling an external provider.

Implementation reconciliation completed on 2026-07-27:

- `PlayerNetworkSetup` exposes an optional avatar-construction delegate, seated-state event/current
  seat access, and XR-safe posture height handling. All paths retain stock behavior when no
  integration subscribes.
- `AvatarSelectionManager.SuppressLegacyAvatars` defaults to `false`; the stock preview path is
  unchanged unless an external integration explicitly assumes ownership.
- `AvatarInputConverter` preserves/restores the complete camera offset across seated correction,
  keeps the seated avatar facing the seat, and exposes a horizontal-only view-correction seam.
- `SitSpot.SeatedPelvisTarget` is optional and includes an editor gizmo; `SitPoint` remains the
  player/root anchor.
- `SceneLoader` raises a provider-neutral transition event, waits up to ten seconds for the
  additive world scene to resolve, and disables world-scene `EventSystem` objects after activation
  so the persistent host scene remains the UI-input owner.
- `AvatarExtensionSync` was moved to
  `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/` with GUID
  `989f755490ee91249b0d5fa7b310dd94` preserved. The generic GHA runtime asmdef no longer references
  `Fusion.Unity`.
- The QuantumVertex `RoomManager` scene-timing call was deliberately excluded because it belongs to
  deferred performance diagnostics and depends on timing APIs not carried into the target.

The five modified core scripts contain no UMA or GHA provider types, `git diff --check` reports no
content errors, and Unity 6000.3 compiles with zero errors or warnings. Phase 4 remains
`PROVISIONAL` only until the stock-only runtime gate is exercised in the target project.

Gate 4:

- core diff is small, generic, and independently reviewable;
- stock avatars work with GHA absent or disabled;
- no UMA type appears in core.

### Phase 5 -- Migrate project wiring and UI

Status: `IN PROGRESS`

Do not copy entire QuantumVertex scenes or prefabs over the newer VertexForm3D versions.
Reapply the required components and references through the Unity editor.

Actions:

1. Wire both player prefabs with the same provider-neutral network and host contracts.
2. Reconcile Home avatar host/customizer wiring.
3. Reconcile seat occupancy, `SitPoint`, and optional `SeatedPelvisTarget` authoring.
4. Assign and verify the GHA locomotion/posture Animator controller.
5. Keep the existing Change Avatar button as the sole avatar-configuration entry point. Do not
   register Avatar as an enabled `UILayoutConfig` Custom entry, because the documented baker turns
   every enabled entry into a bottom-navigation tab.
6. Replace scene-specific UMA UI assumptions with a provider-neutral GHA avatar panel shell.
7. Preserve optional UMA controls as provider-contributed content.
8. Set a distinct Fusion AppVersion before any external add-on deployment.
9. Keep Photon App IDs and credentials local and unstaged.

Player-prefab wiring completed through AnkleBreaker on 2026-07-27:

- `NewGenericMRVRPrefab.prefab` and `NewGenericMRDesktopPrefab.prefab` each carry
  `VertexFormCore.AvatarExtensionSync` and `GHA.AvatarSuite.UmaAvatarBridge` on the root beside the
  existing `PlayerNetworkSetup` and `NetworkObject`.
- Each bridge resolves
  `Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset` and
  `Packages/com.vertexform3d.gha/Runtime/Animations/GHA_Locomotion_v2.controller`.
- Bridge defaults match on both prefabs: local VR cull layer `7`, moving turn speed `720`, idle
  turn speed `200`, and movement-facing threshold `0.6`.
- Unity compiled the result with zero errors or warnings.

Fusion bake verification completed on 2026-08-26 through Unity's supported Pipeline CLI. Each
player prefab was loaded into an isolated prefab-editing scene, processed by Fusion's official
`NetworkObjectBakerEditTime`, inspected, and unloaded without saving. Both
`NewGenericMRDesktopPrefab.prefab` and `NewGenericMRVRPrefab.prefab` returned `hadChanges=false`, 12
network objects, 20 network behaviours, and zero null baked entries. `AvatarExtensionSync` was
present in each baked behaviour table; `UmaAvatarBridge` correctly remained outside the table
because it is not a `NetworkBehaviour`. This supersedes the earlier AnkleBreaker inspection anomaly
(`Index was outside the bounds of the array`, ticket ID `8`) without routing to another project or
modifying either prefab.

The previously blank Photon Fusion AppVersion was set locally to `gha-migration-20260826` on
2026-08-26 so migration test sessions do not share the default matchmaking partition. This changed
only the non-secret AppVersion field through Unity serialization. The credential-bearing
`PhotonAppSettings.asset` remains unstaged and must remain outside any publishable change set.

Current avatar-management UI:

- `UmaAvatarCatalog.asset` has provider-level `Enable Stock Avatars`,
  `Enable UMA Avatars`, and `Default System` fields.
- `Vertex Form > Main UI Database > Right Section > Avatar Datas` manages the stock avatar list,
  but has no per-avatar enabled flag.
- the runtime customizer shows UMA/Classic selection when both providers are enabled.
- `Vertex Form > Creator Toolkit > Avatars` only places the 3D selection rig.

Change Avatar integration corrected through the idempotent GHA installer on 2026-07-28:

- The provider-neutral `GHAAvatarPanel.prefab` is embedded beneath the existing
  `AvatarSelectionManager.customAvatarSelectionUI` station in `HomeSceneComponent.prefab`.
- The existing Change Avatar and close flow is retained. The obsolete station Previous/Next
  controls are hidden because the Stock provider now owns those controls.
- The earlier `UILayoutConfig` Custom Avatar entry was removed and both menu-shell prefabs were
  rebaked. Neither `MainMap.prefab` nor `MainMap Desktop.prefab` contains an Avatar screen or
  bottom-navigation Avatar button.
- A second installer run was a no-op: panel/config/bake/Home-prefab change flags were all false.
- Unity compiled with zero errors.

Reversible integration lifecycle verified on 2026-07-28:

- `Tools > GHA > Integration > Install GHA Host Layer` applies the provider-neutral patch to the
  five VertexForm seam scripts, enables `VERTEXFORM_GHA_HOST`, creates the panel shell, embeds it
  beneath the existing Change Avatar station, and adds `AvatarExtensionSync` to both player
  prefabs.
- `Tools > GHA > Integration > Install UMA Provider Layer` requires the host layer, enables
  `VERTEXFORM_GHA_UMA`, contributes the UMA panel provider and Home preview/host, and adds
  `UmaAvatarBridge` to both player prefabs. It never defines or consumes `UMA_INSTALLED`.
- Uninstall in reverse order. Each layer records exact pre-install snapshots beneath
  `UserSettings`, outside the repository. An unchanged installed asset is restored byte-for-byte;
  if a user changed it after installation, the uninstaller performs a surgical removal of only
  the layer-owned contributions.
- A complete host-plus-UMA install/uninstall audit restored the original Home and player prefabs
  and all five patched host scripts. The only original tracked deltas left after the audit were
  the independent `AddressablesDownloader` fix, independent `SceneLoader` world/EventSystem work,
  and the two tags continuously ensured by UMA's own import processor.
- The final reinstall completed with zero compilation errors. AnkleBreaker serialization checks
  confirmed `AvatarExtensionSync` and `UmaAvatarBridge` on both player roots, all three providers
  on `GHAAvatarPanel.prefab`, `UmaHomeAvatar` on the Home XR rig, and `GHA UMA Preview` at local
  position `(0.9, -0.50, 0)` and local scale `(0.40, 0.40, 0.40)`.

Unity domain reloads triggered by the layer symbols must be run with the editor in the real Windows
foreground on this workstation. Start `Tools/Keep-UnityCompileForeground.ps1` for the target Unity
process before selecting an install/uninstall menu item, wait until AnkleBreaker reports
`isCompiling: false`, then stop that exact helper process. With the helper active before the symbol
change, the final UMA-provider compilation completed in under one minute.

Desktop smoke correction and verification on 2026-07-28:

- The first Login smoke exposed a null in `LoginManager.ConnectToServer`: the live unpacked
  `LoginScene` copy of `ProjectManager.settingsUI` was null even though
  `LoginSceneComponent.prefab` still referenced `SettingsUI.asset`.
- The scene object was explicitly reassigned to
  `Assets/VertexForm3D/ScriptableObjects/SettingsUI.asset` through Unity and the scene was saved.
  Reopening `LoginScene` from disk confirmed that the reference resolves.
- Connect Anonymously then loaded `HomeScene`; Change Avatar opened the provider-neutral Stock/UMA
  panel; Close Avatar returned to Home; the bottom-nav Avatar entry was absent; and the Console
  remained at zero errors for the full path.
- Flat Web/Desktop UMA grounding now accounts for the host `CharacterController.skinWidth`.
  AnkleBreaker measured the pre-fix UMA root/capsule bottom at `Y=-0.102495` and the live floor
  collider at `Y=-0.182495`, an exact `0.08 m` gap matching the Home rig's skin width. The
  first non-XR correction anchored to `capsule bottom - skinWidth`, but visual inspection showed
  the shoe sole slightly penetrating the floor. The current formula adds a serialized
  `flatAvatarGroundClearance` of `0.02 m`, yielding
  `capsule bottom - skinWidth + max(0, clearance)`. It compiles with zero errors; the revised shoe
  placement still requires a visual Desktop/Web retest. WebXR remains on `HumanoidVrAlignment`
  and does not execute this flat-screen correction.

Remaining Phase 5 follow-up:

- visually confirm the revised `0.02 m` flat-platform footwear clearance;
- run the stock-only install/runtime gate after uninstalling the optional UMA provider layer;
- retain provider registration and provider-specific configuration as separate concerns when
  adding future providers.

Gate 5:

- scene/prefab wiring is recreated on the current upstream assets;
- the Home configuration flow works without a scene-specific hard dependency on UMA;
- stock and UMA provider policy can be authored without editing code;
- no credentials are staged.

### Phase 6 -- Runtime validation

Status: `PENDING`

Partial Desktop evidence accepted on 2026-08-26: `LoginScene` anonymous login reached `HomeScene`;
Change Avatar opened the refreshed Avatar Studio; the Classic and Custom providers were both
present; switching Custom -> Classic -> Custom completed; Classic recovered three active preview
renderers; and Custom retained one active preview renderer. Final close-up and player-view captures
are under `Assets/Screenshots/GHA_Avatar_Studio_*_Final.png`. Compilation completed with no errors
and Play Mode was stopped afterward. This does not satisfy stock-only, UMA-only, save/persistence,
spawn/remote, two-client, scene-transition, seating, platform, or device cases. Repeated pre-existing
XR Affordance receiver and `AvatarInputConverter.Update` null-reference errors also remain outside
this UI slice.

Classic control follow-up passed on 2026-08-28 through the same real login/player flow. Next changed
the saved stock index 4 -> 5 and rebuilt the preview as `Head6(Clone)`/`Body6(Clone)`; Previous
returned 5 -> 4 and rebuilt it as `Head5(Clone)`/`Body5(Clone)`. Classic and Custom now both use the
label `SAVE AVATAR` and the same live RectTransform values (176 x 56, bottom-right anchor,
anchored position (-12, 12)). Compilation completed without errors, the final Classic capture was
refreshed, and Play Mode was stopped. Save/persistence beyond this in-session control check remains
part of the pending matrix.

Preview composition follow-up passed on 2026-09-01. Classic uses the provider-neutral
renderer-bounds fit after tab selection and every Previous/Next rebuild, centering each stock model
and uniformly scaling it to 94% of the usable backdrop. UMA uses a provider-specific framing
policy: preserve the authored preview scale (`GHA UMA Preview` at 0.40 and DCA child scale 1.0),
center rendered bounds horizontally, and align the rendered feet to the preview bottom after every
character update. This makes the height slider grow the body upward from planted feet and permits
top overflow. The final card layout moves the provider caption into the top header, extends the
usable preview floor to 3.5% above the actual card edge, and leaves about 8.6 px of visible shoe
clearance at both center and maximum height. Center/max-height horizontal error was 0.081/0.029 px,
and projected height grew from about 247 px to 389 px. The shared Classic/UMA preview-card
backdrop is opaque black. Retained evidence is under
`Assets/Screenshots/GHA_Avatar_Studio_UMA_Position_Check.png`,
`GHA_Avatar_Studio_UMA_Final.png`, and `GHA_Avatar_Studio_UMA_Max_Height_Check.png`;
`GHA_Avatar_Studio_Classic_Final.png` was refreshed. Compilation passed and Play Mode was stopped.

Reference-layout follow-up passed on 2026-09-03. The lower Avatar Studio now has a provider-owned
category rail on the left, opaque-black live preview in the centre, and active controls on the
right. Provider and card backgrounds use generated nine-sliced rounded rectangles; the Classic
Avatar tile and Custom Body/Face/Colors/Outfits tiles have visible borders and selected states.
Provider tabs remain at the top and both providers retain the matching bottom-right `SAVE AVATAR`
action. The live `LoginScene` -> anonymous `HomeScene` pass confirmed preview RGBA `(0, 0, 0, 1)`,
Custom category switching, and Classic Next 4 -> 5 / Previous 5 -> 4. The Console returned zero
errors. Evidence is `Assets/Screenshots/GHA-avatar-studio-reference-layout.png` and
`Assets/Screenshots/GHA-avatar-studio-classic-three-column.png`; compilation passed and Play Mode
was stopped.

Run from `LoginScene` and retain evidence for each case:

| Case | Required result |
|---|---|
| Stock-only policy | Stock selection, spawn, remote rendering, and persistence work without UMA |
| UMA-only policy | UMA selection, customization, save, spawn, and remote reconstruction work |
| Mixed policy | Player can choose UMA or Classic; both render correctly in the same room |
| Home -> Ocean -> Home -> Ocean | Avatar size is invariant; only grounding/IK is re-established |
| Standing start | Stable calibration and eye/sole alignment |
| Seated start | Authored/locked scale is preserved until valid standing calibration |
| Sit/stand | Lower-body posture, pelvis target, camera correction, and visibility transition work |
| Two clients | Recipe, posture, seat, visibility, and authority agree |
| Late join standing/seated | Avatar and posture reconstruct deterministically |
| Disconnect while seated | Stale seat claim recovers |
| Controller/hand switch | Correct hand targets and wrist profile without scale changes |
| HMD sleep/wake | Tracking and constraints recover without moving the world or recalibrating size |
| Local HMD view | Head hidden; body and hands visible |
| Mirror/remote cameras | Full avatar remains visible |
| UMA rebuild | Animator, IK, visibility, diagnostics, and provider bindings reacquire |
| WebGL/WebXR | Provider works and XR loader is enabled where required |
| WebGPU flat | Materials render; no synchronous readback failure |
| Quest standalone | Project builds and the migrated avatar path functions on device; performance tuning is deferred |

Gate 6:

- all required cases pass or have user-approved, documented deferrals;
- no source-only behavior is lost.

### Phase 7 -- Commit and handoff

Status: `PENDING`

Recommended commit sequence:

1. documentation and package scaffolding;
2. minimal VertexForm3D core seams;
3. provider-neutral GHA runtime and assets;
4. UMA provider runtime/editor code;
5. VertexForm host adapters;
6. scene, prefab, Animator, catalog, and UI wiring;
7. diagnostics, validation artifacts, and cleanup.

Before each commit:

- obtain the user's express approval for the specific staging and commit action;
- inspect the exact diff;
- exclude `Assets/UMA`;
- exclude Photon credentials and unrelated project settings;
- ensure renamed/moved Unity assets retain required `.meta` files;
- state which migration ledger rows the commit closes.

The target becomes ready for an upstream PR only when every required ledger row is `DONE` or has an
explicit user-approved deferral.

### Phase 8 -- Post-migration timing and performance

Status: `DEFERRED`

Owner priority override, 2026-09-04: the owner requested resuming investigation of UMA load
blocking after accepting the current UI. Timing investigation is now active under that explicit
request; this does not close the outstanding functional gates or authorize vendor-source changes.
Initial evidence and the required next capture are recorded in
`Logs/MetaOpenXR-Fix-2026-09-04/UMA-PERFORMANCE-BASELINE.md`. No runtime optimization has yet been
applied. The original post-migration policy below remains the default for broader architecture
changes and the remaining platform performance pass.

Approved profiling follow-up, September 4 EDT: normal LoginScene → HomeScene captured a
18.46-second synchronous AssetIndexer/reference-graph load inside DynamicCharacterAvatar.Start.
The index audit found 488 hard references and no addressable entries. Detailed evidence,
limitations, and the proposed catalog-scoped/asynchronous loading direction are in
`Logs/MetaOpenXR-Fix-2026-09-04/UMA-PROFILE-FINDINGS.md`. Play Mode and profiling are stopped.
No runtime optimization or content/build configuration change has been applied yet.

First-two-step investigation, September 4 EDT: `Tools/PlanUmaStreaming.cs` produced a read-only
catalog dependency plan with 76 candidate index records (20 startup, 56 streamable), preserving
one race and all 17 wardrobe entries, with zero missing explicit slot/overlay dependencies.
`Logs/UMA-Streaming/IMPLEMENTATION-PLAN.md` records the existing Resources-index build-inclusion
constraint, scoped local Addressables setup, stock generator side effects, and verification gates.
No streaming configuration or runtime change is active yet.

This phase begins only after the functional migration is complete. Do not change avatar
construction strategy, UMA content policy, or GHA architecture in pursuit of performance while
source parity and runtime behavior are still being established.

Actions:

1. Review the UMA developer's recommendations and record each recommendation with its expected
   benefit, risk, and applicable platform.
2. Establish a repeatable migrated baseline for Home construction, customizer preview,
   save/refresh, player spawn, remote reconstruction, scene transitions, memory, and frame time.
3. Separate UMA generation time, content/import cost, network delay, scene/UI delay, and GHA
   embodiment work before selecting optimizations.
4. Apply one optimization at a time in the owning layer.
5. Re-run the functional validation matrix after each change.
6. Run the dedicated Quest and multi-avatar performance pass.

Performance work is not part of the functional migration definition of done and must not delay
the migration checkpoint.

## Original-plan reconciliation ledger

Update this table as work proceeds. Never delete a row merely because its priority changes.

| Original plan item | Current target status | Required next action |
|---|---|---|
| Stock/UMA interop and author policy | `PROVISIONAL` | Compare against frozen source; validate stock-only, UMA-only, mixed, switching, persistence, and two-client behavior. |
| First-person UMA head renderer split | `PROVISIONAL` | Validate Quest HMD, mirror, remote camera, rebuild, hair/eyes/mouth routing, and two-renderer cost. |
| Remote-avatar proximity hide | `PROVISIONAL` | Validate in VR and with both stock/UMA renderers; fade remains optional. |
| Personal-space collider | `DEFERRED` | Retain as a complementary comfort feature; design must account for room-scale HMD movement. |
| Local VR embodiment | `PROVISIONAL` | Reconcile the latest QuantumVertex calibration behavior and rerun Home/world functional acceptance. |
| Scene-invariant physical fit | `PENDING` | Finish tracking-space calibration validation; persist/replicate locked fit if required. |
| Real walk animation | `PROVISIONAL` | Source was accepted; verify clips, controller state, speed normalization, and packaging in target. |
| Seated posture/network occupancy | `PROVISIONAL` | Run local, proxy, late-join, double-booking, stale-claim, pelvis-target, and camera-correction tests. |
| Customizer V2 DNA/colors/wardrobe | `PROVISIONAL` | Validate catalog contents, slider prefab, preview, save/refresh, provider switching, and two-client persistence. |
| Avatar loading timing investigation | `DEFERRED` | Preserve existing diagnostics, but begin analysis and optimization only after full functional migration. |
| Generic framework extraction | `PROVISIONAL` | Establish all 26-file parity; eliminate accidental provider/host dependencies. |
| Local Humanoid prefab/FBX proof provider | `NOT STARTED` | Implement after generic package parity to prove GHA does not depend on UMA. |
| Provider registry/capabilities | `PENDING` | Define stable provider discovery, readiness, visibility, bounds, rebuild, fit, and configuration contracts. |
| Provider-neutral avatar configuration panel | `PROVISIONAL` | Refreshed shell plus Stock/UMA contributions are installed behind Change Avatar with no main-nav tab. Desktop click, provider switching, and preview construction passed on 2026-08-26; Classic Previous/Next preview rebuilding and exact Classic/Custom save-action parity passed on 2026-08-28; Classic bounds-fit plus authored-scale, bottom-pinned UMA framing and height-slider behavior passed on 2026-09-01. Validate close, save/refresh, persistence, stock-only, UMA-only, spawn/remote, and two-client behavior. |
| Individual avatar enable/disable editor UI | `NOT STARTED` | Add stable IDs and availability flags without list removal/reordering. |
| Locked scale synchronization | `PENDING` | Verify deterministic remote/late-join reconstruction; add explicit sync only if needed. |
| Lower-body leg/foot IK | `DEFERRED` | Design grounded targets and animation-aware weights before adapting RPM patterns. |
| Optical finger embodiment | `NOT STARTED` | Add provider-neutral joint mapping only if required for the first release. |
| Facial/viseme contract and lip sync | `NOT STARTED` | Define generic semantic input and UMA mapping; include Web fallback. |
| UMA material/shader audit | `NOT STARTED` | Audit Editor/URP, Quest, WebGL, and WebGPU by content category; fix only failures. |
| Dual-renderer WebXR/WebGPU delivery | `NOT STARTED` | Re-enable WebXR where required and implement/test runtime renderer/XR gating. |
| Selfie WebGPU asynchronous readback | `NOT STARTED` | Replace synchronous `ReadPixels` path with supported async behavior. |
| Quest standalone performance pass | `DEFERRED` | Run in post-migration Phase 8 after the functional Quest path is established. |
| Fusion AppVersion gating | `NOT STARTED` | Required before external deployment so incompatible NetworkBehaviour layouts cannot share sessions. |
| UMA installation/editor tooling | `PROVISIONAL` | PowerShell sync exists; implement cross-platform Unity Editor workflow after compatible UMA tooling is available. |
| Upstream PR preparation | `DEFERRED` | Do not begin until migration and validation gates pass; keep core, GHA, and UMA changes separable. |
| Login streaming video fix | `PENDING/SCOPE REVIEW` | Determine whether it belongs in the avatar migration or a separate upstream fix; do not silently drop it. |
| Unity Build And Run launcher bug | `DEFERRED/EXTERNAL` | Preserve reporting/workaround notes; not owned by GHA implementation. |

## Migration ledger template

Add one row per migrated unit. Use the frozen source commit and concrete validation evidence.

| Unit | Source path/commit | Target owner/path | Status | Verification | Notes |
|---|---|---|---|---|---|
| Humanoid avatar contracts | `Assets/QVAddons/AvatarSuite/HumanoidAvatarContracts.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Core/HumanoidAvatarContracts.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Provider-neutral; no UMA or VertexForm host dependency. |
| Avatar load timing log | `Assets/QVAddons/AvatarSuite/AvatarLoadTimingLog.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Diagnostics/AvatarLoadTimingLog.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Intentional namespace, cross-assembly accessibility, and marker rename only. Instrumentation retained; analysis remains deferred to Phase 8. |
| Humanoid presentation controller | `Assets/QVAddons/AvatarSuite/HumanoidAvatarPresentationController.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Presentation/HumanoidAvatarPresentationController.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Provider-neutral presentation policy remains in the generic package. |
| Humanoid posture animator | `Assets/QVAddons/AvatarSuite/HumanoidPostureAnimator.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Animation/HumanoidPostureAnimator.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Animator parameter detection and posture reapplication behavior are preserved without provider dependencies. |
| Seated pelvis alignment | `Assets/QVAddons/AvatarSuite/HumanoidSeatedPelvisAlignment.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Embodiment/HumanoidSeatedPelvisAlignment.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Generic visual-root correction remains independent of UMA and VertexForm seat implementation types. |
| XR rig startup stabilizer | `Assets/QVAddons/AvatarSuite/XrRigStartupStabilizer.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Tracking/XrRigStartupStabilizer.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Intentional namespace and diagnostic marker rename only; readiness, floor-origin, and locomotion-release behavior are preserved. |
| Local Addressables startup | `Assets/VertexForm3D/Scripts/AddressablesSystem/AddressablesDownloader.cs @ 994d9aa4` | `Assets/VertexForm3D/Scripts/AddressablesSystem/AddressablesDownloader.cs` | `DONE` | Exact QuantumVertex parity restored for the local-bundle branch; Windows Addressables built successfully with zero player compile errors | When `onlyLocalBundles` is true, initialize the built catalog directly. The negated condition incorrectly attempted to load the literal remote profile expression and blocked `LoginScene`. |
| VertexForm humanoid rig input | `Assets/QVAddons/AvatarSuite/VertexFormHumanoidRigInput.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/VertexFormHumanoidRigInput.cs` | `DONE` | Exact source comparison except intentional namespaces/import; zero-error GHA compile | Correctly remains in host-coupled staging because it depends on `AvatarInputConverter`, `PlayerNetworkSetup`, and `ProjectManager`. |
| Avatar extension network sync | `Assets/QVAddons/AvatarSuite/AvatarExtensionSync.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/VertexForm/Runtime/AvatarExtensionSync.cs` | `DONE` | Behavioral source comparison; GUID `989f755490ee91249b0d5fa7b310dd94` preserved through Unity move; zero-error GHA compile | Only code delta is the correct `new` modifier for inherited `HasInputAuthority`. The Fusion-specific contract now lives in the stable host-integration layer; `Fusion.Unity` was removed from `VertexForm.GHA.Runtime.asmdef`. |
| Avatar camera visibility | `Assets/QVAddons/AvatarSuite/AvatarCameraVisibilityController.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Presentation/AvatarCameraVisibilityController.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Intentional namespace and diagnostic marker rename only; camera culling and XR diagnostic snapshot behavior are preserved. |
| Humanoid tracking lifecycle | `Assets/QVAddons/AvatarSuite/HumanoidTrackingLifecycle.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Tracking/HumanoidTrackingLifecycle.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Tracking loss/regain and delayed reinitialization state remain provider- and host-neutral. |
| Humanoid VR alignment | `Assets/QVAddons/AvatarSuite/HumanoidVrAlignment.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Embodiment/HumanoidVrAlignment.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Grounded root, eye-point correction, floor sampling, and one-time scale behavior are preserved without provider/host imports. |
| VR calibration session | `Assets/QVAddons/AvatarSuite/VrCalibrationSession.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Calibration/VrCalibrationSession.cs` | `DONE` | Behavioral source comparison plus zero-error compile after compatibility fix | New writes use the `GHA` PlayerPrefs key; a valid legacy `QVAS` standing-eye-height value is migrated once so upgrades do not silently discard accepted calibration. |
| VR height calibration | `Assets/QVAddons/AvatarSuite/VrHeightCalibration.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Calibration/VrHeightCalibration.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Provider-neutral one-time scale calculation is preserved. |
| Humanoid arm rig | `Assets/QVAddons/AvatarSuite/HumanoidArmRig.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Embodiment/HumanoidArmRig.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Runtime Animation Rigging setup, target updates, tracking weights, and cleanup remain provider- and host-neutral. |
| Humanoid locomotion driver | `Assets/QVAddons/AvatarSuite/HumanoidLocomotionDriver.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Animation/HumanoidLocomotionDriver.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Root-motion measurement and `Speed`/`Direction` Animator driving remain provider-neutral. |
| Remote avatar proximity hider | `Assets/QVAddons/AvatarSuite/RemoteAvatarProximityHider.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/RemoteAvatarProximityHider.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Namespace/bootstrap-name adaptations only. Correctly remains staged because current discovery imports UMA and VertexForm player/room types; future provider bounds can generalize it. |
| UMA recipe codec | `Assets/QVAddons/AvatarSuite/UmaRecipeCodec.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha.uma/Runtime/Recipes/UmaRecipeCodec.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Wire version, 128-byte capacity, append-only IDs, DNA, and color encoding are preserved in the UMA adapter package. |
| UMA recipe store | `Assets/QVAddons/AvatarSuite/UmaRecipeStore.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha.uma/Runtime/Recipes/UmaRecipeStore.cs` | `DONE` | Behavioral source comparison plus zero-error compile after compatibility/boundary fixes | New GHA recipe/mode keys migrate valid legacy QVAS values once. Stock mode remains explicitly wire-compatible at `0` without importing `AvatarExtensionSync`; the stable VertexForm stock-selection key is preserved as a literal. |
| UMA avatar catalog code | `Assets/QVAddons/AvatarSuite/UmaAvatarCatalog.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Intentional namespace and CreateAssetMenu rename only; provider policy, serialized fields, append-only DNA/color IDs, and accessors are preserved. Catalog asset contents remain a separate project-wiring verification. |
| UMA customizer row | `Assets/QVAddons/AvatarSuite/UmaCustomizerRow.cs @ 994d9aa4` | `Packages/com.vertexform3d.gha.uma/Runtime/UI/UmaCustomizerRow.cs` | `DONE` | Exact source comparison except intentional namespace; zero-error GHA compile | Serialized row roles and UI references are preserved in the UMA adapter package. |
| Avatar load profiler capture | `Assets/QVAddons/AvatarSuite/AvatarLoadProfilerCapture.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Diagnostics/AvatarLoadProfilerCapture.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Namespace, marker, and ignored `Library/GHAProfiles` output-folder rename only. Capture is preserved as evidence; performance analysis remains deferred. |
| Avatar profile report generator | `Assets/QVAddons/AvatarSuite/Editor/AvatarLoadProfileReportGenerator.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Editor/AvatarLoadProfileReportGenerator.cs` | `DONE` | Behavioral source comparison plus zero-error compile after menu cleanup | QVAS names were consistently migrated; redundant `Tools/GHA/GHA` menu path corrected to `Tools/GHA`. Report generation remains deferred to Phase 8. |
| UMA Home avatar host | `Assets/QVAddons/AvatarSuite/UmaHomeAvatar.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaHomeAvatar.cs` | `DONE` | Exact source comparison except intentional namespaces/import; zero-error GHA compile | Correctly remains in host-coupled staging because it coordinates Home rig/controller behavior with the UMA provider. |
| UMA avatar customizer | `Assets/QVAddons/AvatarSuite/UmaAvatarCustomizer.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/UI/UmaAvatarCustomizer.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Intentional namespace/runtime-object rename and duplicate-using cleanup only; tabbed UI, preview, save, provider selection, DNA, colors, and wardrobe behavior are preserved. |
| UMA avatar bridge | `Assets/QVAddons/AvatarSuite/UmaAvatarBridge.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaAvatarBridge.cs` | `DONE` | Exact source comparison except intentional namespaces/import; zero-error GHA compile | Multiplayer provider selection, opaque recipe payload, construction override, visibility, and posture/seat synchronization are preserved in host-coupled staging. |
| UMA avatar puppet | `Assets/QVAddons/AvatarSuite/UmaAvatarPuppet.cs @ 994d9aa4` | `Assets/VertexForm3D/3rdPartyAssets/GHA/Integration/UMA/Runtime/UmaAvatarPuppet.cs` | `DONE` | Behavioral source comparison; zero-error GHA compile | Intentional namespaces, runtime-object name, and diagnostic marker renames only; UMA construction, renderer split, visibility, animation, calibration, tracking, and seated alignment behavior are preserved. |

All 26 frozen QuantumVertex AvatarSuite C# units now have an explicit, closed ledger row.
`AvatarExtensionSync` was moved through Unity into the stable VertexForm host-integration layer
with its GUID preserved, removing the generic GHA runtime assembly's only Fusion dependency.
AnkleBreaker verified the resulting assembly references and Unity reported zero errors or warnings.

Script GUID audit on 2026-07-27: all 26 source `.cs.meta` GUIDs match their unique target
counterparts (`26/26`, zero missing, ambiguous, or mismatched). Existing serialized references to
the moved scripts are therefore preserved.

Non-script AvatarSuite asset audit on 2026-07-27: all 20 frozen `.fbx`, `.controller`, `.asset`,
and `.prefab` files have unique target counterparts and preserved `.meta` GUIDs (`20/20`). Seventeen
are byte-identical. The only content differences are the internal names of
`QVAS_Locomotion`/`QVAS_Locomotion_v2` renamed to `GHA_Locomotion`/`GHA_Locomotion_v2`, and the catalog's serialized class
identifier namespace. Unity resolves the catalog as `GHA.AvatarSuite.UmaAvatarCatalog` with all
expected serialized fields through the preserved MonoScript GUID.

| Unit | Source path/commit | Target owner/path | Status | Verification | Notes |
|---|---|---|---|---|---|
| AvatarSuite animation/model asset set | `Assets/QVAddons/AvatarSuite/Animations/** @ 994d9aa4` | `Packages/com.vertexform3d.gha/Runtime/Animations/**` | `DONE` | `18/18` unique counterparts and GUID matches; 16 byte-identical; controller diffs inspected | The two renamed GHA controllers differ only in internal controller name. Animator parameters, states, transitions, motions, and imported FBX content are preserved. |
| UMA avatar catalog asset | `Assets/QVAddons/AvatarSuite/UmaAvatarCatalog.asset @ 994d9aa4` | `Packages/com.vertexform3d.gha.uma/Runtime/Data/UmaAvatarCatalog.asset` | `DONE` | GUID preserved; serialized diff inspected; Unity ScriptableObject resolved with expected type/properties | Only namespace class identifier differs. Lists and object-reference GUIDs are unchanged; referenced installed UMA content/Global Library validation remains a Phase 2 runtime/editor gate. |
| UMA DNA slider prefab | `Assets/QVAddons/AvatarSuite/UMADNASlider.prefab @ 994d9aa4` | `Packages/com.vertexform3d.gha.uma/Runtime/UI/UMADNASlider.prefab` | `DONE` | GUID and SHA-256 content match | UI prefab serialized content is byte-identical. |

## New-agent start sequence

1. Read `AGENTS.md`, `GHA-SESSION-HANDOFF-2026-08-25.md`, this runbook, package boundaries,
   architecture, and the original QuantumVertex plan.
2. Inspect both repositories' branches, remotes, HEADs, and dirty files without modifying them.
3. Check whether the frozen QuantumVertex commit has been recorded in Phase 0.
4. If it is still `TBD`, create the authorized functional checkpoint without beginning timing
   analysis or performance optimization. Do not claim target parity.
5. Check target compilation. If the AnkleBreaker UMA compatibility blocker remains, report it and
   resolve it before Unity runtime work.
6. Re-run the file inventory against the frozen source commit.
7. Pick one migration ledger row and complete its ownership classification, implementation,
   compile check, and proportionate runtime verification.
8. Update this runbook before moving to another slice.

## Definition of migration complete

The migration is complete only when:

- QuantumVertex is frozen at a recorded commit and treated as read-only;
- every original-plan item is `DONE`, `SUPERSEDED`, or explicitly user-approved as `DEFERRED`;
- every source file and project modification is accounted for;
- GHA and UMA package boundaries are enforced by assemblies and source dependencies;
- UMA can be reproduced from the pinned external checkout without committing vendor code;
- the target passes the agreed Desktop, VR, multiplayer, scene-transition, seating, persistence,
  Web, and functional Quest validation matrix;
- credentials and unrelated changes are excluded;
- commits are separated so VertexForm3D authors can evaluate core, generic GHA, and UMA work
  independently.

Performance analysis and optimization are deliberately outside this definition. They begin only
after the completed functional migration has produced a stable GHA baseline.
