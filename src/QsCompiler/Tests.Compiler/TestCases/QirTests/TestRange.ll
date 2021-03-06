define %Range @Microsoft__Quantum__Testing__QIR__TestRange__body() {
entry:
  %0 = load %Range, %Range* @EmptyRange
  %1 = insertvalue %Range %0, i64 0, 0
  %2 = insertvalue %Range %1, i64 2, 1
  %x = insertvalue %Range %2, i64 6, 2
  %a = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 9)
  %3 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 0)
  %4 = bitcast i8* %3 to i64*
  store i64 0, i64* %4
  %5 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 1)
  %6 = bitcast i8* %5 to i64*
  store i64 2, i64* %6
  %7 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 2)
  %8 = bitcast i8* %7 to i64*
  store i64 4, i64* %8
  %9 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 3)
  %10 = bitcast i8* %9 to i64*
  store i64 6, i64* %10
  %11 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 4)
  %12 = bitcast i8* %11 to i64*
  store i64 8, i64* %12
  %13 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 5)
  %14 = bitcast i8* %13 to i64*
  store i64 10, i64* %14
  %15 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 6)
  %16 = bitcast i8* %15 to i64*
  store i64 12, i64* %16
  %17 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 7)
  %18 = bitcast i8* %17 to i64*
  store i64 14, i64* %18
  %19 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %a, i64 8)
  %20 = bitcast i8* %19 to i64*
  store i64 16, i64* %20
  %b = call %Array* @__quantum__rt__array_slice(%Array* %a, i32 0, %Range %x)
  %21 = load %Range, %Range* @EmptyRange
  %22 = insertvalue %Range %21, i64 0, 0
  %23 = insertvalue %Range %22, i64 1, 1
  %y = insertvalue %Range %23, i64 4, 2
  %start__1 = extractvalue %Range %y, 0
  %step__1 = extractvalue %Range %y, 1
  %end__1 = extractvalue %Range %y, 2
  br label %preheader__1

preheader__1:                                     ; preds = %entry
  %test__1 = icmp sgt i64 %step__1, 0
  br label %header__1

header__1:                                        ; preds = %exiting__1, %preheader__1
  %j = phi i64 [ %start__1, %preheader__1 ], [ %27, %exiting__1 ]
  %24 = icmp sge i64 %j, %end__1
  %25 = icmp sle i64 %j, %end__1
  %26 = select i1 %test__1, i1 %25, i1 %24
  br i1 %26, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %27 = add i64 %j, %step__1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_unreference(%Array* %a)
  call void @__quantum__rt__array_unreference(%Array* %b)
  ret %Range %x
}
