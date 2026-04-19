using System.Collections.Generic;

namespace GlobalDomination.GameData
{
    /// <summary>
    /// Snapshot of units placed on the attack staging grid (4×6, row-major cell indices).
    /// </summary>
    public readonly struct AttackStagingSummary
    {
        public AttackStagingSummary(
            int attackerOwnerId,
            int defenderOwnerId,
            City sourceCity,
            int divisionNumber,
            IReadOnlyList<FortUnitEntry> stagedUnits,
            IReadOnlyList<int> gridCellIndices)
        {
            AttackerOwnerId = attackerOwnerId;
            DefenderOwnerId = defenderOwnerId;
            SourceCity = sourceCity;
            DivisionNumber = divisionNumber;
            StagedUnits = stagedUnits;
            GridCellIndices = gridCellIndices;
        }

        public int AttackerOwnerId { get; }
        public int DefenderOwnerId { get; }
        public City SourceCity { get; }
        public int DivisionNumber { get; }
        public IReadOnlyList<FortUnitEntry> StagedUnits { get; }
        /// <summary>Parallel to <see cref="StagedUnits"/>; index in the 4×6 row-major grid.</summary>
        public IReadOnlyList<int> GridCellIndices { get; }
    }
}
