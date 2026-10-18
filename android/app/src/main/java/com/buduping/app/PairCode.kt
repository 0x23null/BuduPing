package com.buduping.app

/**
 * Pair-code algorithm — MUST match docs/pair-code.md exactly
 * (shared with the Windows app).
 *
 * Alphabet: Crockford Base32 minus I, L, O, U.
 * Code = 8 chars: 7 random payload chars + 1 checksum char at the end.
 * Checksum = alphabet[(sum of the 7 payload indices) mod 32].
 * Displayed as XXXX-XXXX; Firebase pairId is the 8 chars without the dash.
 */
object PairCode {

    private const val ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"

    /**
     * Normalize user input, in spec order:
     * 1. uppercase everything
     * 2. O -> 0, I -> 1, L -> 1
     * 3. keep only alphabet chars (drops '-', spaces, junk)
     */
    fun normalize(input: String): String =
        input.uppercase()
            .replace('O', '0')
            .replace('I', '1')
            .replace('L', '1')
            .filter { it in ALPHABET }

    /** True only when: 8 chars after normalize AND last char matches checksum. */
    fun isValid(input: String): Boolean {
        val raw = normalize(input)
        if (raw.length != 8) return false
        val sum = raw.take(7).sumOf { ALPHABET.indexOf(it) }
        return raw[7] == ALPHABET[sum % 32]
    }

    /** "QJNTEBJ3" -> "QJNT-EBJ3" */
    fun format(raw8: String): String =
        if (raw8.length == 8) raw8.substring(0, 4) + "-" + raw8.substring(4) else raw8
}
