# MoneyTests

Unit tests for the `Money` value object, ensuring correct behavior for currency handling, arithmetic operations, and comparison logic.

## API

### `Constructor_ValidAmountAndCurrency_CreatesInstance`
Verifies that a `Money` instance is successfully created when provided with a valid amount and currency code.

### `Constructor_NegativeAmount_ThrowsDomainException`
Ensures that constructing a `Money` with a negative amount throws a `DomainException`.

### `Constructor_InvalidCurrencyCode_ThrowsDomainException`
Confirms that providing an invalid currency code (not matching ISO 4217) throws a `DomainException`.

### `Constructor_AmountExceedsMaximum_ThrowsDomainException`
Validates that attempting to create a `Money` with an amount exceeding the defined maximum throws a `DomainException`.

### `Constructor_CurrencyCodeNormalizedToUpperCase`
Checks that the currency code is normalized to uppercase during construction, regardless of input casing.

### `Add_SameCurrency_ReturnsSummedAmount`
Tests that adding two `Money` instances with the same currency returns a new instance with the summed amount.

### `Add_DifferentCurrencies_ThrowsDomainException`
Ensures that adding `Money` instances with different currencies throws a `DomainException`.

### `Subtract_SufficientAmount_ReturnsDifference`
Verifies that subtracting a smaller amount from a larger one returns a new `Money` instance with the correct difference.

### `Subtract_WouldResultInNegative_ThrowsDomainException`
Confirms that subtracting an amount larger than the current balance throws a `DomainException`.

### `IsGreaterThan_LargerAmount_ReturnsTrue`
Checks that the `IsGreaterThan` method returns `true` when comparing a smaller amount to a larger one.

### `IsLessThan_SmallerAmount_ReturnsTrue`
Ensures that the `IsLessThan` method returns `true` when comparing a larger amount to a smaller one.

### `Equals_SameAmountAndCurrency_ReturnsTrue`
Validates that two `Money` instances with the same amount and currency are considered equal.

### `Equals_DifferentAmount_ReturnsFalse`
Confirms that two `Money` instances with different amounts are not considered equal.

## Usage
