#if VERTEXFORM_GHA_HOST
using Fusion;
using UnityEngine;

namespace VertexFormCore
{
    /// <summary>
    /// Generic per-player avatar extension state. Carries which avatar system a player
    /// uses (Mode) plus an opaque, versioned data blob for add-on avatar systems
    /// (e.g. a UMA recipe). Deliberately add-on-agnostic and inert by default:
    /// Mode 0 = the stock head/body avatar path, Data unused.
    ///
    /// Designed for adoption into VertexForm3D core unchanged (hence the VertexFormCore
    /// namespace): if every client ships this component on the player prefabs — even
    /// clients with no avatar add-on installed — the Fusion network layouts match, and
    /// vanilla + add-on clients can safely share sessions. Vanilla clients never write
    /// it and render everyone through the stock path; add-on clients read Mode/Data to
    /// render extended avatars, and still see vanilla players as stock (Mode stays 0).
    /// </summary>
    public class AvatarExtensionSync : NetworkBehaviour
    {
        public const int DataCapacity = 128;
        /// <summary>Mode value for the stock VertexForm3D head/body avatar path.</summary>
        public const byte StockMode = 0;
        /// <summary>Canonical provider-neutral standing posture value.</summary>
        public const byte StandingPosture = 0;
        /// <summary>Canonical provider-neutral seated posture value.</summary>
        public const byte SittingPosture = 1;

        [Networked] public byte Mode { get; set; }
        [Networked] public byte Revision { get; set; }
        [Networked] public byte Length { get; set; }
        [Networked, Capacity(DataCapacity)] public NetworkArray<byte> Data { get; }
        [Networked] public byte Posture { get; set; }
        [Networked] public byte PostureRevision { get; set; }
        [Networked] public NetworkId SeatId { get; set; }

        /// <summary>Raised after Spawned — networked state is readable from here on.</summary>
        public event System.Action SpawnedEvent;
        /// <summary>Raised per changed networked property name (e.g. nameof(Mode)) on proxies and owner alike.</summary>
        public event System.Action<string> StateChanged;

        public bool IsSpawned { get; private set; }
        public new bool HasInputAuthority => Object != null && Object.HasInputAuthority;

        private ChangeDetector _changeDetector;

        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            IsSpawned = true;
            SpawnedEvent?.Invoke();
        }

        public override void Render()
        {
            if (_changeDetector == null) return;
            foreach (var change in _changeDetector.DetectChanges(this))
                StateChanged?.Invoke(change);
        }

        /// <summary>
        /// Publishes provider-neutral posture and seat identity, then bumps a transition revision.
        /// The existing player NetworkTransform remains authoritative for the live seat anchor pose.
        /// </summary>
        public bool WritePosture(byte posture, NetworkId seatId)
        {
            if (!HasInputAuthority)
                return false;
            if (posture != StandingPosture && posture != SittingPosture)
            {
                Debug.LogWarning($"[AvatarExtensionSync] Rejected unknown posture value {posture}.");
                return false;
            }

            if (posture == StandingPosture)
                seatId = default;
            if (Posture == posture && SeatId == seatId)
                return false;

            Posture = posture;
            SeatId = seatId;
            PostureRevision++;
            return true;
        }

        /// <summary>Writes the blob and bumps Revision so observers rebuild. Owner only.</summary>
        public void WriteData(byte[] buffer, int length)
        {
            if (buffer == null || length < 0 || length > DataCapacity || length > buffer.Length)
            {
                Debug.LogWarning($"[AvatarExtensionSync] Rejected data write of length {length}.");
                return;
            }
            for (int i = 0; i < length; i++)
                Data.Set(i, buffer[i]);
            Length = (byte)length;
            Revision++;
        }

        /// <summary>Copies the current blob out of the networked array.</summary>
        public byte[] ReadData()
        {
            byte[] buffer = new byte[Length];
            for (int i = 0; i < Length; i++)
                buffer[i] = Data.Get(i);
            return buffer;
        }
    }
}
#endif
