# GHA implementation plan

Status: draft for owner review, September 18, 2026.

Inputs: the roadmap (R1–R14) and known defects (BUG-1–BUG-10) in
[ARCHITECTURE.md](Packages/com.vertexform3d.gha/Documentation~/ARCHITECTURE.md), the packaging
decision recorded there, and the validation matrix in `GHA-MIGRATION-RUNBOOK.md`. This plan
sequences that work by dependency. It does not change any rule in the runbook: Play Mode needs the
owner's approval each time, UMA vendor source is never edited, and nothing is staged, committed or
pushed without approval for the exact action and file scope.

Sizes are rough effort for one person including verification: **S** under a day, **M** one to
three days, **L** a week or more. BUG-3 cannot be sized until its trigger has been captured in the
Editor Console.

## Phase A: stabilize what users will see first

Defect IDs are `BUG-n` (architecture document); plan items are lettered by phase (`A1`, `B1`,
…) and are unrelated to the defect numbers. These are the defects a first-time user hits in Home. They come before packaging because the
package would ship them, and before the upstream pitch because reviewers will try Home first.

| # | Item | Size | Done when | Notes |
|---|---|---|---|---|
| A1 | **BUG-2: DNA sliders not applied to the player or after reload** | S–M | Slider changes appear on the player after Save, and on both player and preview after a restart. `Tools/ValidateUmaHumanBodyTypes.cs` extended with a DNA round-trip check. | **Done on desktop (owner-verified Sep 18).** UMA ignores `predefinedDNA` for new-DNA races; fix writes DNA through the setters in `UmaAvatarPuppet` and `UmaAvatarCustomizer`. Remaining: validation-tool coverage, VR/second-client pass under R13, commit. |
| A2 | **BUG-1: two avatars after choosing Classic on a session that started as UMA** | M | Repro from BUG-1 passes: start with persisted UMA, enter Home, pick Classic, only the Classic avatar is visible; repeat for VR and for a second client. | **Done on desktop (owner-verified Sep 18).** Studio default tab now follows the host default; the legacy build no longer touches the rig while browsing. Core seam changed, host patch regenerated and verified against stock 1.1.9. Remaining: second-client and VR pass under R13, commit. Separate observation: `UmaAvatarBridge.ApplyLocalStockAvatar`/`ApplyLocalRecipe` are never called; harmless in Home but review for networked worlds. |
| A3 | **BUG-3: walk animation missing on first Home load** | needs Console capture | Cause recorded with Console evidence; fix verified over ten cold loads on desktop. | Confirmed by the owner; the trigger is not yet known. Owner will capture the Console on the next occurrence. Until then, add a one-line diagnostic in `HumanoidLocomotionDriver` when suppression is active on a non-VR session so the next occurrence self-reports. |
| A4 | **BUG-4: XR Affordance / `AvatarInputConverter.Update` null references** | S–M | Zero occurrences in a LoginScene → Home → world → Home pass on desktop and PC Link. | Pre-existing since August; may be host code, in which case it becomes part of the upstream contribution. |
| A5 | **BUG-10: PC Link VR regression after VR-only arm IK** | S | Hands follow controllers in VR; arms return to Animator on leaving VR; recorded in the runbook. | Owner-run headset session; needs Play Mode approval. |
| A7 | **BUG-11: rebuild leaves the avatar visible around the first-person camera** | S | After a Save with any change in first person, the head/body stay hidden; verified on desktop and PC Link; `HumanoidRebuilt` fires after rebuilds. | **Done on desktop (owner-verified Sep 18).** Completion handling now runs on every `CharacterUpdated`. Retest BUG-3 on the next cold Home loads, since this refreshes Animator/bone state after rebuilds; PC Link pass under R13. |
| A6 | **R11: jump animation** | S–M | Jump input on desktop triggers a jump state on both prefabs; VR unaffected. | First check whether VertexForm exposes a jump input and whether the supplied clips include a jump. If neither, sourcing a clip is a separate decision. Provider-neutral: `HumanoidLocomotionDriver` plus `GHA_Locomotion_v2`. |

Exit gate: Home smoke-test checklist in the developer guide passes on desktop with the first cold
load included; runbook updated.

## Phase B: make the repository packageable

Everything the user guide's "Work still required" list needs, in an order that keeps the
repository building throughout.

| # | Item | Size | Done when | Depends on |
|---|---|---|---|---|
| B1 | **Move both package folders under `Assets/VertexForm3D/3rdPartyAssets/GHA`** through the Editor; update the 14 hardcoded `Packages/com.vertexform3d.gha*/...` paths; confirm Unity dropped the embedded lock records; compile clean. | M | Both installers rerun byte-identically in the test clone; validation tools pass. | — |
| B2 | **R7: catalog becomes a project asset.** Provider installer copies the catalog on first install and rewires Home avatar, player prefabs and panel to the copy. | M | Fresh install produces a project catalog; body-type authoring edits the copy; package content unchanged after install. | B1 |
| B3 | **R10: version gates.** Host installer checks the VertexForm3D version before applying the patch; provider installer checks the host version; supported versions recorded per release. | S | Mismatched project gets a clear refusal, not a patch error. | — |
| B4 | **Export script**: Editor menu producing the UMA provider `.unitypackage` (and, until upstream lands, the host `.unitypackage`) from fixed path lists that exclude core scripts, installer-modified prefabs, `Assets/UMA` and the generated index. | S–M | Exported packages import into a stock 1.1.9 project and the installers run. | B1 |
| B5 | **Test the UMA v3.05 release package** in a scratch project: imports to `Assets/UMA`, includes `SourceShaders` or not, shader-graph import result vs. the 12 repairs. Decide whether the provider package must ship the repairs. | S | Result recorded in the user guide and `SETUP.md`. | — (needs the 1.08 GB download) |
| B6 | **Regenerate the host patch** against stock 1.1.9 after Phase A and B1, and record the exact baseline version. Decide whether the `AddressablesDownloader` and `SerializedDataBase` branch changes belong in the patch. Found Sep 18: the working tree's `SceneLoader.cs` carries a world-scene resolve timeout change that is not in the shipped patch; decide whether it is GHA scope. | S | Patch applies and reverses cleanly on a fresh clone. | Phase A |
| B7 | **Fresh-clone acceptance of both guides**: developer guide from source; user guide from the exported packages on a stock project. | M | Both recorded in the runbook with commit IDs. | B1–B6 |
| B8 | **Package Updater recovery test**: update VertexForm3D over an installed project and rerun Install GHA Host Layer. | S | Documented in the user guide's Troubleshooting. | B7 |

Exit gate: a user can follow `GHA-getting-started.md` end to end with real downloads.

## Phase C: upstream contribution of the host layer (R12)

| # | Item | Size | Done when | Depends on |
|---|---|---|---|---|
| C1 | **Stock-only runtime evidence**: with the host layer installed and no provider, Classic avatars, networking, seating, scene transitions and two-client behavior match stock. | M | Recorded pass with console evidence; this is the headline exhibit. | Phase A |
| C2 | **Commit separation** per runbook rule 3: core seams, framework, host adapters, project wiring, tests, each independently reviewable. | M | Branch history reviewed by the owner. | B1, B6 |
| C3 | **Contribution package for the maintainers**: the pitch (core-change policy compliance, zero UMA references, no new dependencies, inert without a provider), the diff summary, the evidence from C1, and the updated architecture document. | S–M | Owner approves the text. | C1, C2 |
| C4 | **Open the upstream pull request** to Vertex-Form-3D and track review. | S + review time | Merged, or feedback folded into a follow-up. | C3, explicit Git approval |

The host installer, the host patch and the temporary host `.unitypackage` are retired when C4 lands.

## Phase D: complete the framework

Design commitments from the architecture document. D1 is the prerequisite for the UPM route and
for a clean upstream story; the rest can proceed in parallel once D1 defines the contracts.

| # | Item | Size | Done when | Depends on |
|---|---|---|---|---|
| D1 | **R5: remove direct VertexForm references from the six UMA staging classes and the UMA installer**, via host contracts in the framework implemented by `Integration/VertexForm`. | L | The six classes compile inside the UMA package; `Integration/UMA` is empty or contains only diagnostics. | Phase C scope agreed (contracts must be part of the contribution or a follow-up) |
| D2 | **R2: local Humanoid prefab/FBX proof provider.** | M | A second `IHumanoidAvatarInstance` builds from an imported Humanoid prefab and passes the desktop and VR embodiment checks. | D1 contracts |
| D3 | **R3: provider registry for the mode byte.** | S–M | Fusion host and Home resolve providers through the registry; `UmaAvatarBridge` no longer assumes UMA. | D2 |
| D4 | **R4: proximity hiding through `TryGetRendererBounds`.** | S | Works for both providers without host lookups. | D2 |
| D5 | **R1: physical-fit policy.** Explicit `ProviderProportions` / `UniformScale` / `AuthoredScale` with provider metadata, `AuthoredScale` default for unknown rigs, customizer UX, and networked resolved mode. | L | Each mode verified with UMA and the D2 provider; current implicit uniform scale removed. | D2 |
| D6 | **UPM packaging of the UMA provider** (optional, per the packaging decision). | M | Package installable from a tarball or Git URL on a project that has the upstream host. | C4, D1, B2 |
| D7 | **R15: lip sync.** Framework face-input contract and driver; Photon Voice host adapter (remote Speaker level, local Recorder level); UMA mapping to the race expression system with rebind after rebuilds; Classic path moved onto the same contract. | M | Remote and mirrored UMA mouths move with speech on desktop and PC Link; muted stays still; survives Save Avatar rebuilds. | BUG-11 fix (rebind point); D1 contracts preferred but not required |

## Phase E: content, performance, extensions

| # | Item | Size | Depends on |
|---|---|---|---|
| E1 | **R8: UMA material and content audit** across Editor/URP, Quest, WebGL, WebGPU/WebXR; includes BUG-5, BUG-8 and the `UMA_SG_Diffuse` decision. | L | Phase B (needs the release-package answer) |
| E2 | **R13: remaining validation matrix**: Quest standalone, WebGL/WebGPU/WebXR, two-client late join. | L | Phase A, per-platform builds |
| E3 | **R9: UMA generation performance** (incremental mesh combiner, pre-warmed builds). Not before E2's functional gates, per runbook rule 1. | L | E2 |
| E4 | **R14: sitting extensions** (crouch, seat-height profiles). | M | demand-driven |
| E5 | **R6: remote GLB provider.** | L | D5 |

## Suggested order of work

1. A1 and A2 first: both are small-to-medium with concrete leads, and both are visible to every
   user.
2. B1 and B3 next: they change file locations, so everything after them is written against the
   final layout.
3. A3–A6 as repro and headset time allow, interleaved with B2–B6.
4. B7 and B8, then C1–C4.
5. D1 onward after the upstream review starts, so contract feedback from maintainers shapes it.

## Open decisions for the owner

1. Whether the `AddressablesDownloader` inverted-condition fix and the `SerializedDataBase`
   attribute go into the host patch, a separate upstream fix, or stay out.
2. Whether the host contracts from D1 are part of the first upstream contribution or a second
   one. First is cleaner for UPM later; second keeps the first PR smaller.
3. Whether a jump clip should be sourced if the supplied animation sets have none (A6).
4. Whether D6 (UPM) is wanted at all once the host is upstream.
