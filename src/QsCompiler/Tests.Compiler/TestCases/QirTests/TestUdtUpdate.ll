define { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestUdtUpdate__body(i64 %a, i64 %b) {
entry:
  %0 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, double, i64 }* getelementptr ({ %TupleHeader, double, i64 }, { %TupleHeader, double, i64 }* null, i32 1) to i64))
  %1 = bitcast %TupleHeader* %0 to { %TupleHeader, double, i64 }*
  %2 = getelementptr { %TupleHeader, double, i64 }, { %TupleHeader, double, i64 }* %1, i64 0, i32 1
  store double 1.000000e+00, double* %2
  %3 = getelementptr { %TupleHeader, double, i64 }, { %TupleHeader, double, i64 }* %1, i64 0, i32 2
  store i64 %a, i64* %3
  %4 = call { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* @Microsoft__Quantum__Testing__QIR__TestType__body({ %TupleHeader, double, i64 }* %1, i64 %b)
  %x = alloca { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }*
  store { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* %4, { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }** %x
  %5 = load { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }*, { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }** %x
  %6 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* getelementptr ({ %TupleHeader, { %TupleHeader, double, i64 }*, i64 }, { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* null, i32 1) to i64))
  %7 = bitcast %TupleHeader* %6 to { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }*
  %8 = getelementptr { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }, { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* %5, i64 0, i32 1
  %9 = load { %TupleHeader, double, i64 }*, { %TupleHeader, double, i64 }** %8
  %10 = call %TupleHeader* @__quantum__rt__tuple_create(i64 ptrtoint ({ %TupleHeader, double, i64 }* getelementptr ({ %TupleHeader, double, i64 }, { %TupleHeader, double, i64 }* null, i32 1) to i64))
  %11 = bitcast %TupleHeader* %10 to { %TupleHeader, double, i64 }*
  %12 = getelementptr { %TupleHeader, double, i64 }, { %TupleHeader, double, i64 }* %9, i64 0, i32 1
  %13 = load double, double* %12
  %14 = getelementptr { %TupleHeader, double, i64 }, { %TupleHeader, double, i64 }* %11, i64 0, i32 1
  store double %13, double* %14
  %15 = getelementptr { %TupleHeader, double, i64 }, { %TupleHeader, double, i64 }* %9, i64 0, i32 2
  %16 = load i64, i64* %15
  %17 = getelementptr { %TupleHeader, double, i64 }, { %TupleHeader, double, i64 }* %11, i64 0, i32 2
  store i64 %16, i64* %17
  %18 = getelementptr { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }, { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* %7, i64 0, i32 1
  store { %TupleHeader, double, i64 }* %11, { %TupleHeader, double, i64 }** %18
  %19 = getelementptr { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }, { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* %5, i64 0, i32 2
  %20 = load i64, i64* %19
  %21 = getelementptr { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }, { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* %7, i64 0, i32 2
  store i64 %20, i64* %21
  %22 = getelementptr { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }, { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* %7, i64 0, i32 1
  %23 = load { %TupleHeader, double, i64 }*, { %TupleHeader, double, i64 }** %22
  %24 = getelementptr { %TupleHeader, double, i64 }, { %TupleHeader, double, i64 }* %23, i64 0, i32 2
  store i64 2, i64* %24
  store { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* %7, { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }** %x
  %25 = bitcast { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* %7 to %TupleHeader*
  call void @__quantum__rt__tuple_reference(%TupleHeader* %25)
  %26 = load { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }*, { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }** %x
  %27 = bitcast { %TupleHeader, double, i64 }* %1 to %TupleHeader*
  call void @__quantum__rt__tuple_unreference(%TupleHeader* %27)
  ret { %TupleHeader, { %TupleHeader, double, i64 }*, i64 }* %26
}
