﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QIR
{
    /// <summary>
    /// Static class that contains common conventions for the QIR runtime library.
    /// </summary>
    public static class RuntimeLibrary
    {
        // int functions
        public const string IntPower = "int_power";

        // result functions
        public const string ResultReference = "result_reference";
        public const string ResultUnreference = "result_unreference";
        public const string ResultEqual = "result_equal";

        // string functions
        public const string StringCreate = "string_create";
        public const string StringReference = "string_reference";
        public const string StringUnreference = "string_unreference";
        public const string StringConcatenate = "string_concatenate";
        public const string StringEqual = "string_equal";

        // to-string
        public const string BigintToString = "bigint_to_string";
        public const string BoolToString = "bool_to_string";
        public const string DoubleToString = "double_to_string";
        public const string IntToString = "int_to_string";
        public const string PauliToString = "pauli_to_string";
        public const string QubitToString = "qubit_to_string";
        public const string RangeToString = "range_to_string";
        public const string ResultToString = "result_to_string";

        // bigint functions
        public const string BigintCreateI64 = "bigint_create_i64";
        public const string BigintCreateArray = "bigint_create_array";
        public const string BigintReference = "bigint_reference";
        public const string BigintUnreference = "bigint_unreference";
        public const string BigintNegate = "bigint_negate";
        public const string BigintAdd = "bigint_add";
        public const string BigintSubtract = "bigint_subtract";
        public const string BigintMultiply = "bigint_multiply";
        public const string BigintDivide = "bigint_divide";
        public const string BigintModulus = "bigint_modulus";
        public const string BigintPower = "bigint_power";
        public const string BigintBitand = "bigint_bitand";
        public const string BigintBitor = "bigint_bitor";
        public const string BigintBitxor = "bigint_bitxor";
        public const string BigintBitnot = "bigint_bitnot";
        public const string BigintShiftleft = "bigint_shiftleft";
        public const string BigintShiftright = "bigint_shiftright";
        public const string BigintEqual = "bigint_equal";
        public const string BigintGreater = "bigint_greater";
        public const string BigintGreaterEq = "bigint_greater_eq";

        // tuple functions
        public const string TupleInitStack = "tuple_init_stack";
        public const string TupleInitHeap = "tuple_init_heap";
        public const string TupleCreate = "tuple_create";
        public const string TupleReference = "tuple_reference";
        public const string TupleUnreference = "tuple_unreference";
        public const string TupleIsWritable = "tuple_is_writable";

        // array functions
        public const string ArrayCreate = "array_create";
        public const string ArrayGetElementPtr = "array_get_element_ptr";

        public const string ArrayCreate1d = "array_create_1d";
        public const string ArrayGetElementPtr1d = "array_get_element_ptr_1d";
        public const string ArrayGetLength = "array_get_length";
        public const string ArrayReference = "array_reference";
        public const string ArrayUnreference = "array_unreference";
        public const string ArrayCopy = "array_copy";
        public const string ArrayConcatenate = "array_concatenate";
        public const string ArraySlice = "array_slice";

        // callable-related
        public const string CallableCreate = "callable_create";
        public const string CallableInvoke = "callable_invoke";
        public const string CallableCopy = "callable_copy";
        public const string CallableMakeAdjoint = "callable_make_adjoint";
        public const string CallableMakeControlled = "callable_make_controlled";
        public const string CallableReference = "callable_reference";
        public const string CallableUnreference = "callable_unreference";

        // qubit functions
        public const string QubitAllocate = "qubit_allocate";
        public const string QubitAllocateArray = "qubit_allocate_array";
        public const string QubitRelease = "qubit_release";
        public const string QubitReleaseArray = "qubit_release_array";

        // diagnostics
        public const string Fail = "fail";
        public const string Message = "message";
    }
}
