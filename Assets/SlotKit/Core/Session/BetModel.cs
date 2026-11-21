using System;

namespace SlotKit
{
    // The bet: a line bet picked from the machine's levels, played on every payline
    public sealed class BetModel
    {
        private readonly MachineDefinition machine;

        public int LevelIndex { get; private set; }
        public long LineBet
        {
            get
            {
                return machine.LineBets[LevelIndex];
            }
        }
        public int Lines
        {
            get
            {
                return machine.Paylines.Length;
            }
        }
        public long TotalBet
        {
            get
            {
                return LineBet * Lines;
            }
        }
        public bool IsMin
        {
            get
            {
                return LevelIndex == 0;
            }
        }
        public bool IsMax
        {
            get
            {
                return LevelIndex == machine.LineBets.Length - 1;
            }
        }

        // Raised after the bet changes
        public event Action<BetModel> Changed;

        public BetModel(MachineDefinition machine, int levelIndex)
        {
            this.machine = machine;
            LevelIndex = Clamp(levelIndex);
        }

        public void Increase()
        {
            SetLevel(LevelIndex + 1);
        }

        public void Decrease()
        {
            SetLevel(LevelIndex - 1);
        }

        public void Max()
        {
            SetLevel(machine.LineBets.Length - 1);
        }

        public void SetLevel(int index)
        {
            index = Clamp(index);
            if (index == LevelIndex)
            {
                return;
            }

            LevelIndex = index;
            Changed?.Invoke(this);
        }

        private int Clamp(int index)
        {
            return Math.Max(0, Math.Min(machine.LineBets.Length - 1, index));
        }
    }
}
