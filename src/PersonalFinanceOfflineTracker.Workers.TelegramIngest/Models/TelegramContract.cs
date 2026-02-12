namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

public static class TelegramContract
{
    public const string DefaultCategory = "Uncategorized";

    public static class ErrorCode
    {
        public const string Format = "ERR_FORMAT";
        public const string AmountRequired = "ERR_AMOUNT_REQUIRED";
        public const string AmountInvalid = "ERR_AMOUNT_INVALID";
        public const string AmountRange = "ERR_AMOUNT_RANGE";
        public const string DescriptionRequired = "ERR_DESCRIPTION_REQUIRED";
        public const string DescriptionLength = "ERR_DESCRIPTION_LENGTH";
        public const string CategoryLength = "ERR_CATEGORY_LENGTH";
        public const string DateInvalid = "ERR_DATE_INVALID";
        public const string DateRange = "ERR_DATE_RANGE";
        public const string DuplicateUpdate = "ERR_DUPLICATE_UPDATE";
        public const string Internal = "ERR_INTERNAL";
    }

    public static class Response
    {
        public const string InvalidFormat = "Invalid format. Use: /add <amount> <description> [category] [yyyy-MM-dd]";
        public const string AmountRequired = "Amount is required.";
        public const string AmountInvalid = "Amount must be a positive number with up to 2 decimals.";
        public const string AmountRange = "Amount must be between 0.01 and 9999999.99.";
        public const string DescriptionRequired = "Description is required.";
        public const string DescriptionLength = "Description must be 1-50 chars.";
        public const string CategoryLength = "Category must be 1-30 chars.";
        public const string DateInvalid = "Date must be yyyy-MM-dd.";
        public const string DateRange = "Date is out of allowed range.";
        public const string DuplicateUpdate = "Message already processed.";
        public const string Internal = "Could not process command. Try again later.";
        public const string Help = "Use: /add <amount> <description> [category] [yyyy-MM-dd]";
    }
}
