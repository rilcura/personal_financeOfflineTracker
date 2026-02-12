using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Abstractions;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Services;

public sealed partial class TelegramCommandParser : ITelegramCommandParser
{
    private static readonly DateOnly MinAllowedDate = new(2000, 1, 1);

    public CommandParseResult Parse(string? rawText, DateOnly serverLocalToday)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return Reject(TelegramContract.ErrorCode.Format, TelegramContract.Response.InvalidFormat);
        }

        if (!TryTokenize(rawText, out IReadOnlyList<string> tokens))
        {
            return Reject(TelegramContract.ErrorCode.Format, TelegramContract.Response.InvalidFormat);
        }

        if (tokens.Count == 0)
        {
            return Reject(TelegramContract.ErrorCode.Format, TelegramContract.Response.InvalidFormat);
        }

        if (tokens[0] == "/help")
        {
            return new CommandParseResult(
                true,
                ParsedCommandKind.Help,
                null,
                null,
                TelegramContract.Response.Help,
                "/help");
        }

        if (tokens[0] != "/add")
        {
            return Reject(TelegramContract.ErrorCode.Format, TelegramContract.Response.InvalidFormat);
        }

        if (tokens.Count == 1)
        {
            return Reject(TelegramContract.ErrorCode.AmountRequired, TelegramContract.Response.AmountRequired);
        }

        string amountToken = tokens[1];
        if (!AmountFormatRegex().IsMatch(amountToken))
        {
            return Reject(TelegramContract.ErrorCode.AmountInvalid, TelegramContract.Response.AmountInvalid);
        }

        if (!decimal.TryParse(amountToken, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
        {
            return Reject(TelegramContract.ErrorCode.AmountInvalid, TelegramContract.Response.AmountInvalid);
        }

        if (amount < 0.01m || amount > 9_999_999.99m)
        {
            return Reject(TelegramContract.ErrorCode.AmountRange, TelegramContract.Response.AmountRange);
        }

        if (tokens.Count <= 2)
        {
            return Reject(TelegramContract.ErrorCode.DescriptionRequired, TelegramContract.Response.DescriptionRequired);
        }

        string description = NormalizeWhitespace(tokens[2]);
        if (description.Length == 0)
        {
            return Reject(TelegramContract.ErrorCode.DescriptionRequired, TelegramContract.Response.DescriptionRequired);
        }

        if (description.Length > 50)
        {
            return Reject(TelegramContract.ErrorCode.DescriptionLength, TelegramContract.Response.DescriptionLength);
        }

        string category = TelegramContract.DefaultCategory;
        DateOnly transactionDate = serverLocalToday;

        IReadOnlyList<string> remainder = tokens.Skip(3).ToArray();
        if (remainder.Count == 1)
        {
            if (LooksLikeDateToken(remainder[0]))
            {
                if (!TryParseDate(remainder[0], serverLocalToday, out DateOnly parsedDate, out CommandParseResult? dateError))
                {
                    return dateError!;
                }

                transactionDate = parsedDate;
            }
            else
            {
                string normalizedCategory = NormalizeWhitespace(remainder[0]);
                if (!IsValidCategory(normalizedCategory))
                {
                    return Reject(TelegramContract.ErrorCode.CategoryLength, TelegramContract.Response.CategoryLength);
                }

                category = normalizedCategory;
            }
        }
        else if (remainder.Count == 2)
        {
            string normalizedCategory = NormalizeWhitespace(remainder[0]);
            if (!IsValidCategory(normalizedCategory))
            {
                return Reject(TelegramContract.ErrorCode.CategoryLength, TelegramContract.Response.CategoryLength);
            }

            if (!TryParseDate(remainder[1], serverLocalToday, out DateOnly parsedDate, out CommandParseResult? dateError))
            {
                return dateError!;
            }

            category = normalizedCategory;
            transactionDate = parsedDate;
        }
        else if (remainder.Count > 2)
        {
            return Reject(TelegramContract.ErrorCode.Format, TelegramContract.Response.InvalidFormat);
        }

        ParsedAddCommandDto parsed = new(
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            description,
            category,
            transactionDate);

        string normalizedCommand =
            $"/add {parsed.Amount.ToString("0.00", CultureInfo.InvariantCulture)} \"{parsed.Description}\" \"{parsed.Category}\" {parsed.TransactionDate:yyyy-MM-dd}";

        return new CommandParseResult(
            true,
            ParsedCommandKind.Add,
            parsed,
            null,
            string.Empty,
            normalizedCommand);
    }

    private static bool TryParseDate(
        string rawDate,
        DateOnly serverLocalToday,
        out DateOnly parsedDate,
        out CommandParseResult? error)
    {
        if (!DateOnly.TryParseExact(rawDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
        {
            error = Reject(TelegramContract.ErrorCode.DateInvalid, TelegramContract.Response.DateInvalid);
            return false;
        }

        DateOnly maxAllowedDate = serverLocalToday.AddDays(1);
        if (parsedDate < MinAllowedDate || parsedDate > maxAllowedDate)
        {
            error = Reject(TelegramContract.ErrorCode.DateRange, TelegramContract.Response.DateRange);
            return false;
        }

        error = null;
        return true;
    }

    private static bool LooksLikeDateToken(string token)
    {
        return token.Length == 10 && token[4] == '-' && token[7] == '-';
    }

    private static bool IsValidCategory(string category)
    {
        return category.Length is >= 1 and <= 30;
    }

    private static string NormalizeWhitespace(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        return MultiWhitespaceRegex().Replace(trimmed, " ");
    }

    private static bool TryTokenize(string rawText, out IReadOnlyList<string> tokens)
    {
        List<string> result = [];
        StringBuilder current = new();
        bool inQuotes = false;
        bool currentStartedWithQuote = false;

        for (int i = 0; i < rawText.Length; i++)
        {
            char currentChar = rawText[i];

            if (currentChar == '"')
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    currentStartedWithQuote = current.Length == 0;
                }
                else
                {
                    inQuotes = false;
                }

                continue;
            }

            if (char.IsWhiteSpace(currentChar) && !inQuotes)
            {
                if (current.Length > 0 || currentStartedWithQuote)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    currentStartedWithQuote = false;
                }

                continue;
            }

            current.Append(currentChar);
        }

        if (inQuotes)
        {
            tokens = [];
            return false;
        }

        if (current.Length > 0 || currentStartedWithQuote)
        {
            result.Add(current.ToString());
        }

        tokens = result;
        return true;
    }

    private static CommandParseResult Reject(string errorCode, string message)
    {
        return new CommandParseResult(
            false,
            ParsedCommandKind.Add,
            null,
            errorCode,
            message,
            null);
    }

    [GeneratedRegex(@"^\d+(\.\d{1,2})?$", RegexOptions.Compiled)]
    private static partial Regex AmountFormatRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultiWhitespaceRegex();
}
