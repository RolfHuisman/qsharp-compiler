define i64 @Microsoft__Quantum__Testing__QIR__TestDeconstruct__body({ %TupleHeader, i64, { %TupleHeader, i64, i64 }* }* %a) {
entry:
  %0 = getelementptr { %TupleHeader, i64, { %TupleHeader, i64, i64 }* }, { %TupleHeader, i64, { %TupleHeader, i64, i64 }* }* %a, i64 0, i32 1
  %x = load i64, i64* %0
  %1 = getelementptr { %TupleHeader, i64, { %TupleHeader, i64, i64 }* }, { %TupleHeader, i64, { %TupleHeader, i64, i64 }* }* %a, i64 0, i32 2
  %y = load { %TupleHeader, i64, i64 }*, { %TupleHeader, i64, i64 }** %1
  %b = alloca i64
  store i64 3, i64* %b
  %c = alloca i64
  store i64 5, i64* %c
  %2 = getelementptr { %TupleHeader, i64, i64 }, { %TupleHeader, i64, i64 }* %y, i64 0, i32 1
  %3 = load i64, i64* %2
  store i64 %3, i64* %b
  %4 = getelementptr { %TupleHeader, i64, i64 }, { %TupleHeader, i64, i64 }* %y, i64 0, i32 2
  %5 = load i64, i64* %4
  store i64 %5, i64* %c
  %6 = load i64, i64* %b
  %7 = load i64, i64* %c
  %8 = mul i64 %6, %7
  %9 = add i64 %x, %8
  ret i64 %9
}
