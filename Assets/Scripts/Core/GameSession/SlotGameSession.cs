namespace Scripts.Core.GameSession
{
    public readonly struct BetConfig
    {
        public readonly int TotalBet;

        public BetConfig(int totalBet)
        {
            TotalBet = totalBet < 0 ? 0 : totalBet;
        }
    }

    public class PlayerWallet
    {
        public long Balance { get; private set; }

        public PlayerWallet(long initialBalance)
        {
            Balance = initialBalance < 0 ? 0 : initialBalance;
        }

        public bool TryDeduct(int amount)
        {
            if (amount < 0 || Balance < amount)
            {
                return false;
            }

            Balance -= amount;
            return true;
        }

        public void Credit(long amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Balance += amount;
        }
    }

    public readonly struct SpinTransaction
    {
        public readonly bool Accepted;
        public readonly int BetAmount;
        public readonly long BalanceAfterDeduct;

        public SpinTransaction(bool accepted, int betAmount, long balanceAfterDeduct)
        {
            Accepted = accepted;
            BetAmount = betAmount;
            BalanceAfterDeduct = balanceAfterDeduct;
        }
    }

    public readonly struct SpinSettlement
    {
        public readonly long Payout;
        public readonly long BalanceAfterSettle;

        public SpinSettlement(long payout, long balanceAfterSettle)
        {
            Payout = payout;
            BalanceAfterSettle = balanceAfterSettle;
        }
    }

    public class SlotGameSession
    {
        private readonly PlayerWallet _wallet;
        private BetConfig _betConfig;

        public SlotGameSession(long initialBalance, BetConfig initialBet)
        {
            _wallet = new PlayerWallet(initialBalance);
            _betConfig = initialBet;
        }

        public long Balance => _wallet.Balance;
        public BetConfig CurrentBet => _betConfig;

        public bool TrySetTotalBet(int totalBet)
        {
            if (totalBet < 0)
            {
                return false;
            }

            _betConfig = new BetConfig(totalBet);
            return true;
        }

        public SpinTransaction TryStartSpin()
        {
            int betAmount = _betConfig.TotalBet;
            bool accepted = _wallet.TryDeduct(betAmount);
            return new SpinTransaction(accepted, betAmount, _wallet.Balance);
        }

        public SpinSettlement SettleSpin(long payout)
        {
            _wallet.Credit(payout);
            return new SpinSettlement(payout, _wallet.Balance);
        }
    }
}
