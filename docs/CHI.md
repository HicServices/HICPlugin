# CHI and NHS numbers

## Background

The NHS as a whole uses 10 digit numbers (`NHS numbers') to identify patients. The first two digits indicate which part of the NHS issued that number: for the CHI numbers used in Scotland, 

## Checksum

The final (10th) digit is the checksum, used to detect some forms of corruption in numbers and make typographical errors less likely to result in a valid number other than the one intended.

The sum of each digit multiplied by 10 minus the distance from the start must be congruent to 0 modulo 11; numbers which would require the check digit to be 10 are skipped.

This is very similar to the scheme used by 10 digit ISBNs, except that they use X to represent checksum values of 10.

## Origins

Numbers starting with 999 are reserved for test purposes.

England, Wales and the Isle of Man use NHS numbers starting with either 4, or 6+.

Northern Ireland is allocated all numbers starting 32 to 39 inclusive.

Scotland has 01 to 31; uniquely, the first six digits are derived from the patient's date of birth (ddmmyy), allowing further validation at the expensive of density.


## Scotland-specific details

As an NHS number, verifying the checksum and excluding the reserved ranges (999, 5) appears all that is available.

If you know the NHS number you are considering is specifically Scottish, ie a CHI number rather than any old NHS number, you can also verify the date portion:

DD 01 - 31
MM 01 - 12
YY 00 - 99

Moreover, months only have 31 days, except April, June, September and November, which have 30, and February, which has 29 in leap years and 28 otherwise.

Leap years occur once in every 4, except on centuries (so 1900 was not), except for every fourth century (so 2000 was).

In this context, it is sufficient to observe that only years congruent to 0 mod 4 may contain a 29/02 date.

The penultimate digit is odd for male patients and even for female. Apart from this, the remaining 3 digits (7-9, between the year and the checksum) appear to be arbitrary.

A space is sometimes inserted between the date portion and the remainder.

An added issue for Scottish data is that days 1-9 of each month in this format result in a leading 0; some software such as Microsoft Excel will "helpfully" remove these, leaving a 9 digit "CHI" which is no longer compliant. Removing a leading zero has no effect on the checksum, however.


## Fast searching for CHIs

In the original RDMP implementations, CHI searching was resource intensive. Starting from the beginning of a string, each time a digit was encountered, two possibilities were considered: it could be the first digit of a 10 digit CHI, or the second digit of one which had been truncated to 9 digits. The second digit encountered must then be either the second digit of a two digit day, or the first digit of a two digit month.

Searching in reverse simplifies this task greatly.

The first digit encountered must be the check digit. The next three form the 3 digit disambiguator, then there is an optional space. Two digits of year, two digits of month, then either one or two digits of day.

In this way, every digit is examined precisely once, without ambiguity or backtracking.
