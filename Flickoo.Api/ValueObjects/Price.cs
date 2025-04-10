namespace Flickoo.Api.ValueObjects
{
    public record Price
    {
        private Price() { }

        public Price(decimal amount, string currency)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency cannot be null or empty.", nameof(currency));
            if (currency.Length != 3)
                throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));

            Amount = amount;
            Currency = currency.ToUpper();
        }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{Amount} {Currency}";
        }
    }
}
