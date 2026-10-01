# GHA implementation and separation plan

Planning revision: October 1, 2026 (session handoff; further performance work deferred until after UMA 3.1). Records the owner's agreed direction; implementation
and acceptance gates below are pending unless explicitly marked otherwise. This replaces the
September 18 sequencing. There are six phases, numbered 0–5; there is no Phase 6 in this plan.
The [migration runbook](GHA-MIGRATION-RUNBOOK.md) controls evidence,
ownership and verification; the [architecture](Packages/com.vertexform3d.gha/Documentation~/ARCHITECTURE.md)
retains roadmap R1–R15 and defects BUG-1–BUG-12.

## 1. Intended result

Complete the necessary separation work in the current combined project first. Then keep the
provider-neutral GHA host contribution in this VertexForm3D fork and maintain the UMA integration
in a separate working copy of VertexForm3D. That integration project owns the GHA UMA integration
code and produces its installable `.unitypackage`. UMA itself is an external dependency.

| Role | Owns | Relationship |
| --- | --- | --- |
| Official VertexForm3D | Starter Kit and, if accepted, the GHA host layer | Upstream for host and integration projects |
| This fork / host contribution branch | Generic framework, VertexForm adapters, Classic provider, shared Studio shell and host wiring | Source of the upstream host PR; `Generic-Humanoid-Avatars` can become host-only after preserving the combined baseline |
| VertexForm3D + UMA integration development copy | GHA UMA integration, installer, exporter, developer sync scripts, fixtures and documentation | Uses an identified host revision; publishes the integration package; no committed UMA vendor tree |
| Official UMA distribution | UMA engine and default content | Installed from one of the three routes below |
| Optional custom UMA fork | Deliberate changes to UMA itself | Needed only for UMA customization; outside normal setup |

The integration working copy can use another branch in this fork or a separate development
repository. A separate working directory is recommended to keep Unity imports and configuration
independent. A second GitHub fork is not technically required. Choose the repository name, branch
and visibility when performing the split; this does not block refactoring. No standalone repository
for the GHA UMA integration is required by this plan.

Developers edit integration code in its owning project and export releases there. Test copies
consume packages or explicitly identified development snapshots; do not maintain a second editable
source of integration code. Local/Git UPM dependencies remain a later option.

## 2. UMA sources and compatibility

The preference order for obtaining UMA is:

1. **Unity Asset Store:** preferred user route, using an explicitly supported release.
2. **Official GitHub releases:** supported downloadable UMA Unity package.
3. **Local UMA repository:** retained developer route using `Tools/Sync-Uma.ps1` and related scripts.

This is source-choice guidance, not an automatic retry/replacement sequence. Developers may
intentionally choose route 3. Use one UMA installation per project. The common integration
installer validates installed content from all three routes; runtime code has no acquisition or
Git dependency. Preserve the developer workflow as a maintained first-class route.

The recorded development baseline is UMA v3.05 / commit
`c9204fe475b334162617da5ca923afcc6050e01e`, with 12 destination-only shader repairs from
`f4edf41ba1017b4a745fd1ba7286824332652b7d`. This does not establish acceptance of Asset Store or
GitHub release artifacts. Identical version labels do not prove identical files, GUIDs, shaders or
content. Check available package versions when validating a release. If a channel does not offer
a supported version, explain that and let the user choose another supported source explicitly.

October 1 owner decision: finish separation and establish working behavior on the current pinned
UMA baseline. Upgrade to UMA 3.1 at the end of that work, before inviting additional testers or
publishing a release (P5-4, §9). The owner reports 3.1f1 released and 3.1f2 bug fixes in progress;
verify the available patch releases and fixes when this gate begins, then select and pin the
reviewed 3.1 patch. This records the intended target, not verified release availability or an
instruction to upgrade now.

Create one integration-owned compatibility record containing:

- Supported VertexForm3D, GHA host/API, Unity/URP and UMA versions, plus network compatibility.
- Per accepted source: artifact identity/version, validation evidence, archive checksum when
  available, required installed asset identities/GUIDs and assembly/API checks.
- For the local route: reviewed commit, branch/clean-tree rules and precise repair profile.
  Preserve dry-run, confirmation, replacement backup and pin checks.
- Required Humanoid races, T-poses/base recipes, wardrobe, color channels, shaders and
  `SourceShaders` requirements, established by inspecting each artifact.

Keep reported provenance separate from verified compatibility. Installed files may not reveal
whether they came from the Asset Store or GitHub. Record supplied provenance as declared and
unknown provenance honestly; never infer it from a folder name. Compatibility must be verified
through reliable release metadata and required-content checks. If insufficient, stop with the
exact missing evidence. Do not silently substitute versions, sources, body types or recipes.
An experimental-version bypass is not part of the supported path.

## 3. Current evidence and gaps

Planning inspected local HEAD `8c19b5f7`. Commits `8a8c86ff` and `8c19b5f7` contain the
September 18 DNA/rebuild and Classic coexistence fixes. Desktop owner checks are recorded;
VR, network and platform gates remain open. Older notes saying these need committing are
historical. Local remote-tracking refs contain them; this does not verify today's GitHub state
or establish a newer upstream baseline.

Source inspection identified these separation dependencies:

- Classic sends `SavedAvatarApplyRequested`; its current listener is in `UmaHomeAvatar`.
  Host-only Save needs its own owner before removing UMA.
- `UmaAvatarBridge` owns the player construction override and provider-specific network handling.
  `AvatarExtensionSync` already transports opaque bytes and must remain provider-neutral.
- There is a UI provider registry, but no runtime registry mapping mode IDs to avatar construction.
- `GhaIntegrationBootstrap` includes both layer menus. Shared panel/Home/player prefabs contain
  UMA components and references. Removing scripts alone would leave broken assets.
- UMA staging classes still access VertexForm concrete types; the generic asmdef alone does not
  prove the complete host independent.
- Installer detection currently checks for a UMA source file, not the full release compatibility
  contract. The developer sync script enforces its Git pin.
- Both package folders remain under `Packages/`; the final layout/export work is pending.

The worktree also contains owner configuration, credentials and import changes. Classify actual
diffs before checkpointing. Preserve the documented owner removal of `com.unity.collab-proxy`;
assess its publication scope separately. Preserve local VR settings while keeping committed
platform defaults aligned with the reviewed upstream.

## 4. Ownership and proposed layout

Move Unity assets through the Editor with their `.meta` GUIDs and assembly names preserved.
Proposed root: `Assets/VertexForm3D/3rdPartyAssets/GHA/`.

| Current area | Proposed destination / owner | Required boundary |
| --- | --- | --- |
| `Packages/com.vertexform3d.gha` | `Framework/` — host | Humanoid contracts, animation, tracking, calibration, presentation, shared Studio; no UMA/Fusion implementation dependency |
| `Integration/VertexForm` | Existing host adapter area | Home/player orchestration, Classic Save/apply, rig input, opaque Fusion transport and registry consumers |
| UMA package and staging | `Providers/UMA/Runtime`, `UI`, `Editor`, `Data`, `Documentation` — integration | UMA construction, recipes, DNA, wardrobe and controls; consume host contracts |
| Host bootstrap, installer and patch | `Installer/Editor/Host/` and provider-neutral shared utilities | Transitional host deployment; no UMA menu/type/dependency check |
| UMA bootstrap/installer | `Providers/UMA/Editor/` | Validate installed host/UMA, create project configuration, add provider wiring |
| Studio/Home/player prefabs | Host base; integration's additive UMA wiring | Host baseline contains only host components; provider installer reproduces additions |
| Rounded Studio chrome | Framework resources | Shared presentation |
| Category icons and sphere swatches | UMA resources | Current consumers are UMA controls; preserve resource lookups when moving |
| Catalog template | UMA data | Create project-owned editable copy; preserve existing IDs and references |
| Sync, validation and UMA profiling scripts | Integration developer tooling | Maintained; excluded from runtime exports |
| UMA vendor content and generated library | External/project-owned | Excluded from integration commits/exports |

Audit animation/model redistribution rights; include only needed assets. Classify files by
responsibility and consumers, not just by their names. Exact move and export manifests are Phase 2
deliverables; the destinations above are planned, not current paths.

## 5. Implementation sequence and tracking table

**Keep the original IDs.** A1–E5 retain their September 18 meanings; their letters no longer
set execution order. The numbered phases below are the current sequence. New separation tasks
use `P<phase>-<item>` IDs. An item spanning phases keeps one ID and is referenced at each gate.
`R` and `BUG` numbers still refer to the architecture roadmap and defect register.

Status: **Pending** = not accepted; **Partial** = bounded evidence exists, with work remaining;
**Deferred** = deliberately later; **Optional** = not committed scope. A Desktop pass does not
close a VR/network item. The runbook remains the authority for acceptance evidence. Original
rough effort estimates are retained in the later-work register; re-estimate refactors once their
scope is concrete rather than carrying old estimates forward as commitments.

| Current phase | Original IDs assigned here | New work |
| --- | --- | --- |
| 0 — Baseline | A5 baseline; C2 checkpoint preparation | P0-1–P0-3 |
| 1 — Lifecycle and contracts | A1, A2, A4, A5, A7; D1–D4; C1 first proof | P1-1 network boundary |
| 2 — Layout and installation | B1–B3, B6 | P2-1 additive assets; P2-2 manifests/licenses |
| 3 — Acquisition and packages | B4, B5, B7, B8; E1 artifact viability | P3-1 compatibility/preflight; P3-2 developer sync |
| 4 — Physical separation | C2 completion; C1 repeat proof | P4-1–P4-3 |
| 5 — Contribution and release | C3, C4; E2 applicable release gates | P5-1–P5-4 |
| Later / deferred / optional (§7) | A3, A6; D5–D7; E1–E5 remaining scope | Additional tracked follow-ups |

Keep runtime refactors, file moves and upstream/package upgrades independently verifiable.

September 26 verification decision: use the owner's prior successful avatar-editing and Save
checks as baseline evidence. Do not require duplicate runs merely to begin the separation work.
Retest affected behavior after relevant implementation changes, or when a new failure creates a
specific reason to do so. Record earlier owner verification separately from newly captured runs;
this does not establish unreported network, tracking-recovery or VR-to-Desktop results.

### Phase 0 — establish a reproducible baseline

| ID | Work | Status | Done when / next gate |
| --- | --- | --- | --- |
| P0-1 | Reconcile HEAD, owner changes and exact host/integration/unrelated file inventory | Partial — September 23 inventory recorded | Owner reviews the classified scope and baseline revision. Preserve unrelated work; exclude credentials, vendor content and generated state from the checkpoint. |
| P0-2 | Verify official upstream comparison revision | Verified September 23 | Official Master was `58bf4e775653c8befbc6d36584742360dbc7033a` (1.1.9). Review any later upstream delta separately; no automatic upgrade during separation. |
| P0-3 | Record compile/import status and Desktop regression baseline | Complete for the bounded current-Editor/Desktop baseline | Results are recorded in the baseline report; clean import/build and broader acceptance remain separate later gates. |
| A5 | BUG-10: PC Link baseline and VR/desktop arm handoff | Partial — September 24 initial Home observations recorded | Owner-run headset pass records controller-following hands and return to Animator arms after VR exit. Carry this check through Phase 1. |
| C2 | Prepare a scoped, reviewable local checkpoint | Partial — owner authorized current-branch local checkpoints October 1; implementation saved as `1e0cec7d`, with companion documentation checkpoint | This preserves current work for transfer, not final Phase 4 commit separation or completed acceptance. See the current session handoff for exact scope and files remaining outside the checkpoints. No push is authorized. |

Evidence: [September 23 baseline](GHA-SEPARATION-BASELINE-2026-09-23.md). Use isolated validation
copies where needed; a blanket reset/cleanup of this directory is unnecessary.

Exit: baseline revision, reviewed inventory and concrete regression expectations. Phase 0 remains
open while owner review and PC Link baseline evidence are outstanding.

### Phase 1 — stabilize lifecycle and finish host contracts (R2–R5)

| ID | Work | Status | Done when / dependency |
| --- | --- | --- | --- |
| A1 | BUG-2: DNA applies to saved player and survives reload | Partial — fixes committed; Desktop rechecked September 23 | Save/reopen/restart and Male/Female/Male round trips remain correct through extraction; extend `ValidateUmaHumanBodyTypes` coverage and complete VR/second-client checks under E2. |
| A2 | BUG-1: Classic/UMA coexistence and authoritative Save | Partial — fixes committed; Desktop rechecked September 23 | Browsing changes the preview only; Save changes the saved body without duplicates. Pass VR/second-client checks and trace the uncalled `ApplyLocalStockAvatar` / `ApplyLocalRecipe` paths. Home mirror behavior alone is not network-save evidence. |
| A7 | BUG-11: first-person visibility after rebuild | Partial — fixes committed; repeated Save checked on Desktop | Head/body hiding and rebuild notification/rebinding survive every Save, including PC Link. Retain this behavior during D1 extraction. |
| A4 | BUG-4: XR Affordance / `AvatarInputConverter.Update` null references | Open — September 27 world spawn produced eight pointer-capacity errors in VR and four in Desktop | Capture exceptions and compare stock before assigning cause; zero occurrences over LoginScene → Home → world → Home on Desktop and PC Link. Diagnose hand/controller activation overlap on the actual spawned prefab; see runbook. Separate an independent upstream fix if appropriate. |
| A5 | BUG-10: PC Link regression | Open — September 27 world body hidden 28.65 s after build, awaiting height calibration | Complete headset tracking/visibility and VR-exit checks, including seated start, saved calibration reuse and Home/world handoff. Previous general VR success does not close this gate; see runbook failure evidence. |
| D1 | R5: extract host lifecycle and remove concrete VertexForm dependencies from UMA | In progress — Home/Classic source extraction; eight Edit Mode checks and bounded Desktop Home construction pass; full lifecycle acceptance remains open | Host adapters own Home/player orchestration, Classic Save/apply, saved-vs-preview state, authority, spawn/despawn, transitions, seating and legacy suppression. UMA staging classes and installer consume contracts, without VertexForm singletons/concrete player types. |
| D3 | R3: runtime mode-to-provider registry | Pending — develop with D1 and D2 | Home and Fusion construction use the registry alongside the UI registry. Classic Save works with no UMA listener. Preserve mode IDs, payload compatibility and saved choices; reject duplicate IDs and define unavailable-provider behavior. |
| D4 | R4: generic proximity hiding | Pending | Policy uses instance bounds/visibility (including `TryGetRendererBounds`) for both providers, without provider-side host discovery. Depends on D1/D2; added comfort features stay later. |
| D2 | R2: minimal local static-avatar proof provider | Pending — linked discussion reviewed September 27; asset assessment remains | Assess an existing RPM, Character Creator or other licensed Humanoid asset; no asset is selected yet. A minimal provider exercises the shared lifecycle and Desktop/PC Link embodiment to validate D1/D3 before freezing contracts. Retain useful code for the later full static-avatar package. |
| P1-1 | Host-owned opaque Fusion transport and compatibility | Pending | Validate lengths, authority, actual Save propagation and incompatible-client behavior using isolated test AppVersions. Preserve baked NetworkBehaviour layout unless a separate protocol change is reviewed; exercise two clients and late join. |
| C1 | Host + Classic runtime with UMA truly absent | Pending — first proof; repeat after split | Clean project contains no UMA dependency or serialized references; Classic navigation, preview, Save/restart, spawn, seating, transitions and two-client behavior match stock. Disabling a define with UMA still present is insufficient. |

D1 contracts follow actual call sites: host context/rig inputs, readiness, cancellation, instance
ownership, build/rebuild/disposal and opaque saved payloads. UMA retains DCA construction, recipe
decoding, DNA, wardrobe/colors, renderer mapping and rebuild notifications. Split installer
bootstraps as part of this boundary; shared snapshot/state utilities cannot hardcode UMA.

D3 preserves Classic mode 0 and the existing UMA mode ID, recipe versions, catalog indices and
preferences. Never overwrite a saved selection merely because its provider is unavailable.
Matching network components alone does not prove stock/provider clients can share a session.

For D2, **static means fixed appearance**, not fixed movement or pose. Animation, tracking and
other supported embodiment behavior remain available; in-application body/wardrobe customization
is outside this provider's scope. At the Phase 1 entry review, read the owner's
[related static-avatar discussion](codex://threads/01a0c97b-c915-7b41-8020-2c62d65371d2)
and reconcile its requirements with D1/D3 and the proof asset choice before implementing D2.
The linked discussion was reviewed September 27; fixture selection and implementation remain pending. Keep the immediate test small;
the complete static-avatar package remains later work rather than a prerequisite to finishing UMA.

D2 asset source located September 23: the existing reference project at
`E:\src\Unity\6000.3\vertexform3d-unity-vr-starterkit-Dev - RPM`, already documented in
[REFERENCE-REVIEW.md](Packages/com.vertexform3d.gha/Documentation~/REFERENCE-REVIEW.md).
Local candidates relative to that project:

| Candidate | Asset path | Read-only file evidence |
| --- | --- | --- |
| RPM `68899e9ba5d9e1a75bf05e46` | `Assets/RPM/Ready Player Me/Avatars/68899e9ba5d9e1a75bf05e46/68899e9ba5d9e1a75bf05e46.glb` | GLB 2; 9 meshes, 1 skin, 67 joints; no external image/buffer URIs |
| RPM `654a18ede5e941858d67b65c` | `Assets/RPM/Ready Player Me/Avatars/654a18ede5e941858d67b65c/94e345af3f4c13d17cee5010ab8c9360/654a18ede5e941858d67b65c.glb` | GLB 2; 1 mesh, 1 skin, 67 joints; no external image/buffer URIs |

Also located `Assets/RPM/Scene/68ca0bea6e1393f194b87b6e.prefab` and the older rigs under
`Assets/RPM/Prefabs/VR/`. The file checks establish local model availability, not Unity Humanoid
validity or GHA compatibility. At Phase 1, assess candidate rig mapping, materials, bounds,
visibility and permitted use before choosing one. Use the avatar asset with the shared GHA
lifecycle; preserve the reference project and do not transplant its complete XR/network rig.
No avatar files have been copied into GHA or selected for distribution.

**A3 walking is owner-deferred (§7).** Capture evidence if it recurs; this does not defer PC Link
or close general locomotion acceptance.

Exit: host + Classic work without UMA installed; UMA and a non-UMA Humanoid exercise the same
lifecycle. Provider code no longer reads concrete VertexForm host types.

### Phase 2 — final layout and installation ownership (R7, R10)

| ID | Work | Status | Done when / dependency |
| --- | --- | --- | --- |
| B1 | Move framework/provider folders through Unity | Pending — after Phase 1 boundary proof | Preserve GUIDs/assembly names; audit every hardcoded path, Resources lookup, asmdef, document and validation script. Reconcile embedded package records through Unity's package workflow; clean compile and repeatable installer reruns. The old count of 14 paths is not the audit limit. |
| P2-1 | Host-only panel/Home/player baselines and additive provider wiring | Pending | Provider install/remove owns only its registrations/components, preserves owner edits and needs no pre-modified host assets; no stale references or missing scripts. |
| B2 | R7: project-owned catalog | Pending — B1 | First install copies the template and rewires consumers; migrate current assignments without changing IDs/recipes. Preserve existing index precedence; create only a missing library and reject an invalid occupied path. |
| B3 | R10: host/API version gates | Pending | Refuse incompatibility before changing symbols/assets. Upstream host runs without an installer-defined host symbol; temporary patch route is only for explicitly supported older releases. |
| B6 | Audit core deltas and regenerate transitional host patch | Pending — Phase 1, B1 and reviewed scope | Classify extension seams, Addressables condition, serialization attribute, scene-resolution timeout, Cesium remnants, OpenXR flags and package versions. Isolate independent fixes/local settings; verify patch apply/reverse against the exact stock revision. |
| P2-2 | License audit and explicit move/export manifests | Pending | Record host/provider ownership, required asset redistribution rights and export exclusions; include only needed assets. Supplies B4 and the physical split. |

Save installer prefabs under a non-WebGL target to avoid the recorded conditional
`MixedRealityHandler` field serialization churn.

Exit: stable GUIDs, clean compilation, additive install/rerun/uninstall checks and preserved user
catalog/index/saved choices. No new UMA source dependency in the host.

### Phase 3 — validate three sources and produce installable packages

| ID | Work | Status | Done when / dependency |
| --- | --- | --- | --- |
| B5 | Validate actual UMA release artifacts | Pending — source pin alone is not artifact acceptance | Obtain chosen Asset Store and official GitHub packages; record identity, versions, APIs, GUIDs, default content and shader/`SourceShaders` requirements. Test v3.05 candidate; evaluate newer candidates separately. Account/import steps may require the owner. |
| E1 | R8 / BUG-5 / BUG-8: artifact viability and shader repair decision | Pending — B5; full audit stays in §7 | Inspect each artifact before applying any of the 12 Git-overlay repairs. Needed repairs are documented, version/source scoped, reversible and reproducible without a source clone; otherwise mark that artifact unsupported. |
| P3-1 | Common compatibility record and installed-content preflight | Pending — B3/B5 | Implement section 2's contract for all three routes, keeping acquisition separate. Test missing/unsupported versions, incomplete content, duplicate installations and declared versus verified provenance. |
| P3-2 | Maintain local-source developer sync | Existing workflow; integration changes pending | Connect `Sync-Uma.ps1` and related checks to common validation; preserve commit/repair pins, dry-run, confirmation, replacement backup and clean source checkout. Custom/unreleased UMA needs a reviewed development compatibility record. |
| B4 | Fixed-manifest package exporter | Pending — B1/P2-2 | Export integration and, if needed, temporary host packages. Inspect archives; exclude vendor content, credentials, generated libraries/caches, unrelated files, direct core-script replacements and complete installer-modified host prefabs. No recursive dependency export. |
| B7 | Fresh-project acceptance of user and developer guides | Pending — B1–B6, P3-1/P3-2 | Test each source in its own clean project; record exact revisions/artifacts. Repeat install/remove and saved-data checks; user package route needs no UMA checkout and developer source route remains maintained. |
| B8 | VertexForm Package Updater recovery | Pending — B7 | Update an installed project, rerun the applicable supported installation path, preserve settings/catalog/saved data and document recovery in the user guide. |

Exit: all three routes have recorded results for the agreed release. Once the host is upstream,
user installation has no Git prerequisite. Document temporary host requirements explicitly.

### Phase 4 — preserve the combined baseline and separate the projects

| ID | Work | Status | Done when / dependency |
| --- | --- | --- | --- |
| C2 | Complete reviewable commit separation and preserve combined baseline | Pending — checkpoint preparation started in Phase 0 | Owner reviews core seams, framework, adapters, wiring, tests and integration changes as scoped local commits; preserve the combined revision under an approved branch/tag. No history rewrite is required. |
| P4-1 | Designate and seed the integration working copy | Pending — owner choice at split | Record repository/branch/visibility and exact host revision; retain integration code/scripts there, acquire UMA externally. A standalone provider repository is not required. |
| P4-2 | Remove integration-owned content from the host tree | Pending — manifests and combined baseline | Use ordinary removal/move/refactor commits; retain host assets/framework, make host guides specific and link optional UMA users to the integration project. Final contribution diff is host-only; earlier UMA history remains. |
| P4-3 | Verify integration installation after split | Pending — P4-1/P4-2 | Installer recreates the combination; compare file/GUID inventories and behavior with the preserved baseline. Each project has one editable source of truth. |
| C1 | Repeat actual host-absence acceptance | Pending — resulting host copy | Re-run clean import/compile and stock-only runtime/network evidence on the physical host tree, with no UMA runtime/editor dependencies or serialized references. |

Exit: independently usable host and integration copies. Names/visibility and Git actions are
reviewed at this phase. Replay, cherry-pick, rebase or force-push are not prerequisites; handle a
maintainer-requested squash separately if needed.

### Phase 5 — contribute the host and release the integration

| ID | Work | Status | Done when / dependency |
| --- | --- | --- | --- |
| C3 | Contribution package for maintainers (R12) | Pending — C1/C2 and final host diff | Owner approves purpose/core-change policy, exact dependency delta against actual upstream, asset/license inventory, architecture and Classic/absence/network evidence. State supported platform scope. |
| E2 | R13: applicable release acceptance gates | Pending — matrix in §6 | Pass the claimed platform/network gates or obtain explicit documented owner scope deferrals. Wider platform coverage stays open; separation alone is not product acceptance. |
| C4 | Upstream PR and review | Pending — C3 and separate explicit Git approval | Present exact final diff targeting official upstream; submit only when authorized, then track acceptance or address feedback. **Current instruction: no pushes or PR publication.** |
| P5-1 | Validate accepted upstream and retire temporary host path | Pending — upstream acceptance | Verify exact accepted revision and update compatibility before retiring the patch for that release. Retain older paths only while intentionally supported; opening a PR is insufficient. |
| P5-2 | Maintain integration against reviewed host changes | Pending — designated integration copy | Bring reviewed fixes from the owning host branch into the integration copy; keep UMA project content out of the host contribution. |
| P5-4 | Final UMA 3.1 migration and regression gate | Pending — owner requested October 1; after separation and working current-version baseline | Select and pin a reviewed 3.1 patch, migrate the integration, and complete §9 verification before additional testers or public release. |
| P5-3 | Publish integration release | Pending — P5-4, accepted artifacts/guides and explicit publication approval | Publish packages, supported-source/version records, checksums and release notes for the verified scope. No publication is currently authorized. |

Track upstream acceptance and package release separately. An approved integration release can
use the verified temporary GHA Host package before upstream accepts the host contribution.
Phase 3 exports and tests two GHA-owned packages: the host and the UMA integration. UMA itself
is obtained separately; it is not re-exported as part of either package.

## 6. Acceptance matrix

| Gate | Evidence | Timing |
| --- | --- | --- |
| Host without UMA | Clean import/compile; no UMA asset/script/assembly dependencies; Classic navigation, preview, Save/restart, spawn, seating, scene transitions | Pre-work and actual split |
| Generic contract proof | Non-UMA Humanoid built through shared lifecycle; Desktop animation and PC Link tracking/visibility | Before freezing contracts |
| UMA lifecycle | Male/Female/Male, DNA/colors/outfits Save/reopen/restart; repeated Classic/Custom switching; single preview/saved body; rebuild visibility/rebind | Pre-work and package smoke pass |
| Network | Two clients, authority, actual Save propagation, remote and late-join appearance/posture, stale-seat cleanup and protocol checks | Host and integration separately |
| XR/scale | PC Link hands/head/mirror, tracking loss/resume, sit/stand and Home/world transitions, invariant scale and Desktop arms after XR exit | Pre-work regression |
| Acquisition | Separate Asset Store, GitHub package and local-source installs; default content, preflight refusal and settings retention | Before split |
| Distribution | Export/import, repeat install, removal, catalog persistence, supported VertexForm update recovery | Before package release |
| UMA 3.1 migration (P5-4) | Reviewed patch/artifact pins, saved-avatar compatibility, Desktop/VR/network regressions and rebuilt package acceptance (§9) | After current-version separation; before additional testers or public release |
| Wider platforms | Windows player, Quest standalone, WebGL/WebGPU/WebXR build/device/material evidence | Before claiming each platform supported |

Record revisions/artifacts, Unity/platform, steps, results and failures. Runtime checks start from
LoginScene; agent-driven Play Mode requires owner approval. Use targeted Editor checks and
owner-run regression steps; do not introduce an always-running runtime validation class.
The complete product matrix remains open until passed or explicitly deferred by the owner.
Separating code does not establish full product acceptance.

## 7. Later work and deferred items — original IDs retained

These items remain tracked without blocking the physical split unless an acceptance gate needs
them. E1/E2 have early gates in §5 and retain their IDs for the remaining coverage here. Sizes
are the original rough estimates: **S** under a day, **M** one to three days, **L** a week or more;
reassess before scheduling.

| ID | Item / original size | Status | Done when / dependency |
| --- | --- | --- | --- |
| A3 | BUG-3: walking missing on first Home load / unsized | **Owner-deferred September 23** | Owner has seen it and another person reported it, but reliable reproduction/cause are unresolved. Capture evidence if it recurs; resume diagnosis later, then verify the fix over ten Desktop cold loads. The old proposed diagnostic change is also deferred. |
| A6 | R11: jump animation / S–M | Deferred until after split | Inspect existing jump input/clips before deciding whether to source a clip. Shared locomotion triggers jump on both Desktop prefabs; VR remains unaffected. |
| D5 | R1: physical-fit policy / L | Deferred until after split — D2 | Explicit `ProviderProportions`, `UniformScale`, `AuthoredScale`; provider metadata and unknown-rig `AuthoredScale` default, customizer UX and deterministic networked resolved policy/scale. Verify with UMA and proof provider; replace implicit uniform scale. Existing scale regressions stay pre-work. |
| D6 | Optional UPM provider packaging / M | Optional — C4, D1, B2 | If selected, validate tarball/Git install against accepted host. No local/Git UPM prerequisite for the current package plan. |
| D7 | R15: lip sync / M | Deferred until after split — D1, A7 | Shared face-input contract, Photon Voice remote Speaker/local Recorder adapter, UMA expression mapping/rebind and Classic support. Speech visible remotely/in mirrors on Desktop/PC Link; muted stays still; Save rebuilds preserve it. |
| E1 | R8: full material/content audit, BUG-5/BUG-8 / L | Pending — Phase 3 artifact check first | Cover Editor/URP, Quest, WebGL/WebGPU/WebXR and `UMA_SG_Diffuse`; record device/content evidence before affected support claims. |
| E2 | R13: remaining platform/network matrix / L | Pending — functional and per-platform builds | Complete Quest standalone, WebGL/WebGPU/WebXR, two-client/late-join and remaining §6 gates. Baseline and release gates occur earlier; do not defer all network verification until after split. |
| E3 | R9 / BUG-9: UMA generation performance / L | Deferred by owner October 1 — revisit after P5-4 UMA 3.1 upgrade | Preserve the local 100-record scoped-index experiment and evidence: both races/all 24 UI wardrobe entries, 31 construction cases passed. Same-project fresh-Editor pair: pre-build interval 5.57 -> 0.56 s, total 7.19 -> 6.47 s, largest observed frame 6.93 -> 1.92 s. Stalls remain. After the upgrade, remeasure and agree any remaining optimization; async loading and device performance experiments are deferred. UMA 3.1 is not assumed to fix this. |
| E4 | R14: sitting extensions / M | Deferred; demand-driven | Scope and verify crouch/seat-height profiles. Preserve current sit/stand behavior in the pre-work. |
| E5 | R6: remote GLB provider / L | Deferred until after split — D5 | Separately scope remote loading and verify against the proven provider contracts and fit policy. |

Additional follow-ups retain their architecture/defect references where available. A5.1/A5.2 are individually tracked follow-ups under A5. Adding P5-4 brings the plan from 45 to 46 parent items (31 original and 15 separation/release items); existing IDs are unchanged:

| Reference | Work | Disposition / completion condition |
| --- | --- | --- |
| BUG-6, BUG-7, BUG-12 | Tree, VRKeys, remote UI pointers | Recheck chosen upstream; separate fixes/reports; resolve if they block a gate. |
| D2 follow-up | Full user-facing static-avatar package | Later product scope, informed by the linked discussion and retained D2 proof code; the Phase 1 fixture establishes only contract proof. |
| D4 follow-up | Additional proximity comfort features | Later; necessary generic bounds/visibility boundary remains Phase 1. |
| R7 follow-up | Per-avatar enable/disable authoring | Later; preserve IDs rather than deleting/reordering catalog entries. |
| A5.1 | VR arms intersect the torso | Owner-deferred September 24; observed in the UMA Home PC Link check. Later assess arm targets, elbow hints and avatar fit; reduce avoidable torso penetration in normal hand poses while preserving controller alignment. Recheck after Save/rebuild and with the D2 proof avatar. Cause and implementation remain unconfirmed. |
| A5.2 | First-person eye/body alignment feels too far back | Owner-deferred September 24; looking down exposes too much upper chest and the owner wants a view farther forward toward the feet. Assess eye landmarks and avatar-to-tracked-head alignment before choosing an offset; verify natural floor/feet view, stable tracking, mirror appearance and Save/rebuild behavior. No camera or body offset has been changed. |
| Embodiment | Optical fingers, lower-body IK, personal-space collider | Later shared embodiment/provider capability work. |
| E2 follow-up | WebXR/WebGPU delivery and selfie async readback | Separate platform work; verify before affected support claims. |
| P1-1 / E2 | Fusion AppVersion compatibility | Test isolation in Phase 1 and compatibility gate before release. |
| P3-2 follow-up | Cross-platform developer tooling | Maintain Windows sync now; other developer shells later. Package route is validated separately. |
| Host/tooling follow-up | Login video / Build And Run issue | Separate scope review; do not silently add to the host PR. |

**Owner decision at session close, October 1:** defer further E3 / R9 / BUG-9 performance
investigation and optimization until after the P5-4 UMA 3.1 upgrade. Continue the separation on
the current pinned version and preserve the smaller-index experiment. After upgrading, compare
first appearance and worst-frame timing with the retained evidence before choosing more work.
The upgrade may change performance; no improvement is promised. The captures below are historical
evidence, not instructions to resume profiling now.

October 1 captured VR repeat: avatar ready in 10.2 s, with 2.2 s synchronous index/reference
loading and a 6.77 s main-thread render wait while the render thread spent 7.08 s uploading
textures. Desktop comparison also captured the stall: avatar ready in 9.3 s, synchronous asset
reads 2.15 s and texture upload 6.68 s, with a 9.19 s main-thread frame. The defect is not limited
to XR. These overlapping thread timings must not be added together. Confirm actual headset
performance after the upgrade in a device build, separating first launch, same-session
reload and app restart. Editor/cache differences do not establish a standalone regression or fix.

October 1 owner regression concern: earlier evidence includes 7.7–8.3 s Home loads and
September 24 builds around 1.2–1.3 s before the lifecycle extraction (with separate VR
calibration visibility delay). The controlled October 1 Desktop Home comparison is now complete:
uncontaminated current runs took 10.4–11.6 s and pre-extraction runs 10.8–13.0 s. A 17.7 s current
run is retained but flagged because an Editor query during loading added 2.3 s; its asset reads
were also slower. Both implementations read approximately 1.27 GB and spent 7.4–8.1 s uploading
textures. No consistent Home-extraction slowdown was reproduced, but same-process Editor
repeats do not rule out a cold/VR regression or other separation changes. E3 remains unresolved
and is now DEFERRED until after P5-4. Current source was restored after the comparison. The later
scoped-index experiment below reduced the dependency set, but did not establish a complete fix
or persistent cache. Device first launch, same-session reuse and app restart performance work
is deferred with E3. Historical streaming work was an audit, not activated streaming.

## 8. Immediate next slice and maintenance

**Resume separation/lifecycle work under D1; do not continue performance experiments this session
or on the new machine before P5-4.** Preserve the original identifiers and review each phase with
the owner at the agreed boundary. The updated
[session handoff and machine-transfer checklist](GHA-SESSION-HANDOFF-2026-08-25.md) records the
current source, setup requirements, private evidence and remaining acceptance gaps. Transfer both
local checkpoints and the required private/local files; a remote clone alone omits unpushed work.

October 1 scoped-index test: the owner authorized the current-UI resource reduction.
`AssetIndexerProject.asset` is now active locally with 100 records; the full vendor index is
preserved. Both races/all 24 clothing entries remain available; 31 construction checks passed.
The initial loading interval fell substantially, but total first appearance was still 6.47 s
and a 1.92 s observed frame remains. Background loading is not enabled. Treat this as an
experiment with visual/device coverage, async loading and installer/build ownership still incomplete;
do not run a generic full-library rebuild over the curated index. See the dated runbook table.

October 1 cause isolation: HomeScene synchronously loads the global UMA index and its broad
asset-reference graph before character construction. The Home-only probe separated 5.63 s of
asset loading from 0.019 s of initialization and a subsequent 1.42 s avatar build. The index
dependency graph includes 464 texture paths; loaded matching textures accounted for about
1.28 GB in the Editor memory estimate. This identifies an excessive startup-content load,
with earlier captures also showing texture-upload stalls. The temporary probe moves the cost;
it is not a fix. Scoped runtime content and selected-recipe asynchronous loading are candidates
to reassess after UMA 3.1, not approved immediate work. Normal avatar
startup is in HomeScene after Connect Anonymously, not in LoginScene. See the runbook's cause
isolation section; preserve the historical comparison limitations below.

October 1 full historical-worktree follow-up: both historical and current code can build a fresh
Home avatar in about 0.46 s when Home is reloaded within the same Play session. New Play sessions
took 3.28/3.37 s; first Play after Editor restart took 6.46/13.75 s. Keep the first-load discrepancy
open: the projects run from different drives and the rebuilt historical index has 20 fewer
persistent project-wide entries. This is not an exact reconstruction of September 24 or proof
that the separation did/did not cause a first-load regression. Any post-upgrade attribution work must hold
index contents and storage/cache conditions constant. See the runbook's full worktree-test
section. Current code is restored in the original project; the scoped index is the subsequent
local experiment, not a change to runtime loading code.

Owner plan review: Phases 0–3 and Phase 5 content approved; Phase 4 conditionally accepted.
These are plan decisions, not completed acceptance checks or commit/publication
approval. Re-review each upcoming phase with the owner when the preceding phase is complete,
and confirm or adjust its scope before starting. Include the linked static-avatar discussion in
the Phase 1 review.

September 27: the owner's instruction to proceed authorizes the first D1 implementation slice.
Subsequent owner direction authorizes stopping/restarting as needed and prefers Desktop testing
now, with Quest 2 available for later PC Link work. Desktop startup/Home has been checked after
the switch. The owner reports working Desktop behavior without noticeable delays, except pointer
errors; Ocean Villa is confirmed Desktop with no active XR. Four new world-spawn pointer errors
keep A4 open, and return Home is not separately confirmed. VR timing remains unverified after
these changes; the earlier 28.65-second calibration visibility delay stays open under A5.
Home/Classic lifecycle source now lives in `VertexFormHomeAvatar`; `UmaHomeAvatar` is an optional
build adapter with compatibility entry points. Compilation and eight isolated Edit Mode checks
pass; focused Desktop runtime verification is authorized and in progress. This does not close
D1, D3, C1, the remaining PC Link observations or documentation/commit review.
The linked static-avatar discussion was read: D2 will use a prepared, imported Humanoid model,
fixed appearance with normal animation/tracking, a stable model ID and explicit head visibility.
Runtime file selection, hosted RPM services and the full static-avatar plugin remain outside D2.
Keep proposed commits for core hooks, host lifecycle, UMA adaptation, moves, install/version checks,
exporters and validation independently reviewable. This does not require rewriting old history.

Keep the plan (sequence), runbook (evidence), architecture/boundaries (ownership), UMA setup
contract (compatibility), user guide (validated downloads) and developer guide (maintained scripts)
synchronized. Git staging/commits/pushes and PRs still require owner approval of exact scope;
the September 27 instruction authorizes the current implementation work, not Git actions or the
repository split.

The upstream Notion links in `AGENTS.md` were attempted but could not be retrieved in this pass.
This plan uses repository source and recorded architecture. Recheck those pages before implementing
host contract/asset changes and reconcile against inspected upstream code.

## 9. Final pre-release step — migrate to UMA 3.1 (P5-4)

Continue the separation and current regression work on the existing pinned UMA version. Once
that work is complete and working, re-review this final gate with the owner and perform it in
the integration working copy. It belongs to Phase 5, before additional testers or public release;
it does not introduce a Phase 6 or renumber existing work.

Further E3 / R9 / BUG-9 performance work is explicitly deferred until after this upgrade. Preserve
the October 1 scoped-index results as a comparison point. This deferral does not waive functional
or required release/platform checks. After the upgrade, assess remaining stalls with the owner
before resuming optimization; do not assume a 3.1 patch resolves them.

| Step | Completion evidence |
| --- | --- |
| Select the 3.1 patch | Review official release notes and the reported f1/f2 fixes; record the selected patch, source commit/artifact and checksum. Do not assume an unreleased patch is available or compatible. |
| Preserve and migrate the baseline | Retain a recoverable current-version baseline; update sync pins, compatibility/preflight records and any version-specific repair profile. Review API, content, shader and GUID changes; rebuild the Global Library without copying generated state. |
| Verify existing avatars and behavior | Clean compile and catalog validation; existing saved recipes/choices, DNA/colors/wardrobe, Classic/UMA switching, Home/world/return, Desktop and PC Link visibility/calibration, and two-client/late-join behavior. Recheck first-load and body-visibility timing against recorded evidence. |
| Revalidate the deliverables | Repeat relevant Phase 3 source-route and clean-project install/export checks for 3.1; verify the host remains usable without UMA, regenerate packages, and update supported-version records and guides. Complete E2 gates for every claimed release platform. |

Earlier current-version checks remain baseline evidence, not acceptance of the upgraded release.
Keep P5-4 open if upgrade regressions or required checks remain unresolved. No dependency update,
new staging, commit, push or release is authorized by this documentation change.
