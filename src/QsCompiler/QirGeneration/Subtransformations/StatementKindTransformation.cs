﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Quantum.QIR;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Ubiquity.NET.Llvm.Instructions;
using Ubiquity.NET.Llvm.Types;
using Ubiquity.NET.Llvm.Values;

namespace Microsoft.Quantum.QsCompiler.QIR
{
    using QsResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using ResolvedExpression = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using ResolvedInitializerKind = QsInitializerKind<ResolvedInitializer, TypedExpression>;

    internal class QirStatementKindTransformation : StatementKindTransformation<GenerationContext>
    {
        public QirStatementKindTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation) : base(parentTransformation)
        {
        }

        public QirStatementKindTransformation(GenerationContext sharedState) : base(sharedState)
        {
        }

        public QirStatementKindTransformation(SyntaxTreeTransformation<GenerationContext> parentTransformation, TransformationOptions options) : base(parentTransformation, options)
        {
        }

        public QirStatementKindTransformation(GenerationContext sharedState, TransformationOptions options) : base(sharedState, options)
        {
        }

        // private helpers

        /// <summary>
        /// Binds a SymbolTuple, which may be a single Symbol or a tuple of Symbols and embedded tuples,
        /// to a Value.
        /// If the SymbolTuple to bind is structured, the Value will point to an LLVM structure with
        /// matching structure.
        /// </summary>
        /// <param name="symbolTuple">The SymbolTuple to bind</param>
        /// <param name="symbolValue">The Value to bind to</param>
        /// <param name="symbolType">The Q# type of the SymbolTuple</param>
        /// <param name="isImmutable">true if the binding is immutable, false if mutable</param>
        private void BindSymbolTuple(SymbolTuple symbolTuple, Value symbolValue, ResolvedType symbolType, bool isImmutable)
        {
            // Bind a Value to a simple variable
            void BindVariable(string variable, Value value, ResolvedType type)
            {
                if (isImmutable)
                {
                    this.SharedState.RegisterName(variable, value, false);
                }
                else
                {
                    var ptr = this.SharedState.CurrentBuilder.Alloca(this.SharedState.LlvmTypeFromQsharpType(type));
                    this.SharedState.RegisterName(variable, ptr, true);
                    this.SharedState.CurrentBuilder.Store(value, ptr);
                }
            }

            // Bind a structured Value to a tuple of variables (which might contain embedded tuples)
            void BindTuple(ImmutableArray<SymbolTuple> items, ImmutableArray<ResolvedType> types, Value val)
            {
                Contract.Assert(items.Length == types.Length, "Tuple to deconstruct doesn't match symbols");
                var itemTypes = types.Select(this.SharedState.LlvmTypeFromQsharpType).ToArray();
                var tupleType = this.SharedState.Types.CreateConcreteTupleType(itemTypes);
                var tuplePointer = this.SharedState.CurrentBuilder.BitCast(val, tupleType.CreatePointerType());
                for (int i = 0; i < items.Length; i++)
                {
                    var item = items[i];
                    if (item.IsDiscardedItem || item.IsInvalidItem)
                    {
                        // Nothing to do
                    }
                    else
                    {
                        var itemValuePtr = this.SharedState.GetTupleElementPointer(tupleType, tuplePointer, i + 1);
                        var itemValue = this.SharedState.CurrentBuilder.Load(itemTypes[i], itemValuePtr);
                        BindItem(item, itemValue, types[i]);
                    }
                }
            }

            // Bind a Value to an item, which might be a single variable or a tuple
            void BindItem(SymbolTuple item, Value itemValue, ResolvedType itemType)
            {
                if (item is SymbolTuple.VariableName v)
                {
                    BindVariable(v.Item, itemValue, itemType);
                }
                else if (item is SymbolTuple.VariableNameTuple t)
                {
                    BindTuple(t.Item, ((QsResolvedTypeKind.TupleType)itemType.Resolution).Item, itemValue);
                }
            }

            BindItem(symbolTuple, symbolValue, symbolType);
        }

        /// <summary>
        /// Generate the allocations and bindings for the qubit bindings in a using statement.
        /// </summary>
        /// <param name="binding">The Q# binding to process</param>
        private void ProcessQubitBinding(QsBinding<ResolvedInitializer> binding)
        {
            ResolvedType qubitType = ResolvedType.New(QsResolvedTypeKind.Qubit);
            IrFunction allocateOne = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.QubitAllocate);
            IrFunction allocateArray = this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.QubitAllocateArray);

            // Generate the allocation for a single variable
            void AllocateVariable(string variable, ResolvedInitializer init)
            {
                Value allocation;
                if (init.Resolution.IsSingleQubitAllocation)
                {
                    allocation = this.SharedState.CurrentBuilder.Call(allocateOne);
                    this.SharedState.ScopeMgr.AddQubitValue(allocation, qubitType);
                }
                else if (init.Resolution is ResolvedInitializerKind.QubitRegisterAllocation reg)
                {
                    this.Transformation.Expressions.OnTypedExpression(reg.Item);
                    var countValue = this.SharedState.ValueStack.Pop();

                    allocation = this.SharedState.CurrentBuilder.Call(allocateArray, countValue);
                    this.SharedState.ScopeMgr.AddQubitValue(
                        allocation,
                        ResolvedType.New(QsResolvedTypeKind.NewArrayType(qubitType)));
                }
                else
                {
                    allocation = Constant.UndefinedValueFor(this.SharedState.Types.Qubit);
                }
                this.SharedState.RegisterName(variable, allocation);
            }

            // Generate the allocations for a tuple of variables (or embedded tuples)
            void AllocateTuple(ImmutableArray<SymbolTuple> items, ImmutableArray<ResolvedInitializer> types)
            {
                Contract.Assert(items.Length == types.Length, "Initialization list doesn't match symbols");
                for (int i = 0; i < items.Length; i++)
                {
                    AllocateItem(items[i], types[i]);
                }
            }

            // Generate the allocations for an item, which might be a single variable or might be a tuple
            void AllocateItem(SymbolTuple item, ResolvedInitializer itemInit)
            {
                switch (item)
                {
                    case SymbolTuple.VariableName v:
                        AllocateVariable(v.Item, itemInit);
                        break;
                    case SymbolTuple.VariableNameTuple t:
                        AllocateTuple(t.Item, ((ResolvedInitializerKind.QubitTupleAllocation)itemInit.Resolution).Item);
                        break;
                }
            }

            AllocateItem(binding.Lhs, binding.Rhs);
        }

        /// <summary>
        /// Generate QIR for a Q# scope, with a specified continuation block.
        /// The QIR starts in the provided basic block, but may be generated into many blocks depending
        /// on the Q# code.
        /// The QIR generation is wrapped in a ref counting scope; temporary references created in the scope
        /// will be unreferenced before branch to the continuation block.
        /// </summary>
        /// <param name="block">The LLVM basic block to start in</param>
        /// <param name="scope">The Q# scope to generate QIR for</param>
        /// <param name="continuation">The block where execution should continue after this scope,
        /// assuming that the scope doesn't end with a return statement</param>
        private void ProcessBlock(BasicBlock block, QsScope scope, BasicBlock continuation)
        {
            this.SharedState.SetCurrentBlock(block);
            this.SharedState.ScopeMgr.OpenScope();
            this.Transformation.Statements.OnScope(scope);
            var isTerminated = this.SharedState.CurrentBlock?.Terminator != null;
            this.SharedState.ScopeMgr.CloseScope(isTerminated);
            if (!isTerminated)
            {
                this.SharedState.CurrentBuilder.Branch(continuation);
            }
        }

        /// <summary>
        /// Generate QIR for a Q# scope, with a specified continuation block.
        /// The QIR starts in the current basic block, but may be generated into many blocks depending
        /// on the Q# code.
        /// This method does not open or close a reference-counting scope.
        /// </summary>
        /// <param name="block">The LLVM basic block to start in</param>
        /// <param name="scope">The Q# scope to generate QIR for</param>
        /// <param name="continuation">The block where execution should continue after this scope,
        /// assuming that the scope doesn't end with a return statement</param>
        private void ProcessUnscopedBlock(BasicBlock block, QsScope scope, BasicBlock continuation)
        {
            this.SharedState.SetCurrentBlock(block);
            this.Transformation.Statements.OnScope(scope);
            if (this.SharedState.CurrentBlock?.Terminator == null)
            {
                this.SharedState.CurrentBuilder.Branch(continuation);
            }
        }

        // public overrides

        public override QsStatementKind OnAllocateQubits(QsQubitScope stm)
        {
            this.SharedState.ScopeMgr.OpenScope();
            this.ProcessQubitBinding(stm.Binding); // Apply the bindings and add them to the scope
            this.Transformation.Statements.OnScope(stm.Body); // Process the body
            this.SharedState.ScopeMgr.CloseScope(this.SharedState.CurrentBlock?.Terminator != null);
            return QsStatementKind.EmptyStatement;
        }

        // We treat borrowing the same as allocating for now.
        public override QsStatementKind OnBorrowQubits(QsQubitScope stm)
        {
            return this.OnAllocateQubits(stm);
        }

        /// <exception cref="InvalidOperationException">The current function or the current block is set to null.</exception>
        public override QsStatementKind OnConditionalStatement(QsConditionalStatement stm)
        {
            if (this.SharedState.CurrentFunction == null || this.SharedState.CurrentBlock == null)
            {
                throw new InvalidOperationException("the current function or the current block is set to null");
            }

            var clauses = stm.ConditionalBlocks;
            // Create the "continuation" block, used for all conditionals
            var contBlock = this.SharedState.AddBlockAfterCurrent("continue");

            // if/then/elif...else
            // The first test goes in the current block. If it succeeds, we go to the "then0" block.
            // If it fails, we go to the "test1" block, where the second test is made.
            // If the second test succeeds, we go to the "then1" block, otherwise to the "test2" block, etc.
            // If all tests fail, we go to the "else" block if there's a default block, or to the
            // continuation block if not.
            for (int n = 0; n < clauses.Length; n++)
            {
                var test = clauses[n].Item1;

                // Evaluate the test, which should be a Boolean at this point
                this.Transformation.Expressions.OnTypedExpression(test);
                var testValue = this.SharedState.ValueStack.Pop();

                // The success block is always then{n}
                var successBlock = this.SharedState.CurrentFunction.InsertBasicBlock(
                            this.SharedState.GenerateUniqueName($"then{n}"), contBlock);

                // If this is an intermediate clause, then the next block if the test fails
                // is the next clause's test block.
                // If this is the last clause, then the next block is the default clause's block
                // if there is one, or the continue block if not.
                var failureBlock = n < clauses.Length - 1
                    ? this.SharedState.CurrentFunction.InsertBasicBlock(this.SharedState.GenerateUniqueName($"test{n + 1}"), contBlock)
                    : (stm.Default.IsNull
                        ? contBlock
                        : this.SharedState.CurrentFunction.InsertBasicBlock(
                                this.SharedState.GenerateUniqueName($"else"), contBlock));

                // Create the branch
                this.SharedState.CurrentBuilder.Branch(testValue, successBlock, failureBlock);

                // Get a builder for the then block, make it current, and then process the block
                var thenScope = clauses[n].Item2.Body;
                this.SharedState.OpenNamingScope();
                this.ProcessBlock(successBlock, thenScope, contBlock);
                this.SharedState.CloseNamingScope();

                // Make the failure current for the next clause
                this.SharedState.SetCurrentBlock(failureBlock);
            }

            // Deal with the default, if there is any
            if (stm.Default.IsValue)
            {
                this.SharedState.OpenNamingScope();
                this.ProcessBlock(this.SharedState.CurrentBlock, stm.Default.Item.Body, contBlock);
                this.SharedState.CloseNamingScope();
            }

            // Finally, set the continuation block as current
            this.SharedState.SetCurrentBlock(contBlock);

            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnExpressionStatement(TypedExpression ex)
        {
            this.Transformation.Expressions.OnTypedExpression(ex);
            // The Value computed is now on top of the stack. We need to pop it, even though it's Unit,
            // since Unit is represented as a null TuplePointer.
            this.SharedState.ValueStack.Pop();

            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnFailStatement(TypedExpression ex)
        {
            // Release any resources (qubits or memory) before we fail.
            this.SharedState.ScopeMgr.ExitScope();

            this.Transformation.Expressions.OnTypedExpression(ex);
            var message = this.SharedState.ValueStack.Pop();

            this.SharedState.CurrentBuilder.Call(this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.Fail), message);
            // Even though this terminates the block execution, we'll still wind up terminating
            // the containing Q# statement block, and thus the LLVM basic block, so we don't need
            // to tell LLVM that this is actually a terminator.

            return QsStatementKind.EmptyStatement;
        }

        /// <exception cref="InvalidOperationException">The current function is set to null.</exception>
        public override QsStatementKind OnForStatement(QsForStatement stm)
        {
            if (this.SharedState.CurrentFunction == null)
            {
                throw new InvalidOperationException("current function is set to null");
            }

            Value PopAndRegister(string name, bool isMutable = false)
            {
                Value value = this.SharedState.ValueStack.Pop();
                this.SharedState.RegisterName(name, value, isMutable);
                return value;
            }

            // Loop variables
            var startName = this.SharedState.GenerateUniqueName("start");
            var stepName = this.SharedState.GenerateUniqueName("step");
            var endName = this.SharedState.GenerateUniqueName("end");
            var testName = this.SharedState.GenerateUniqueName("test");

            // The array to iterate through, if any
            (Value, ITypeRef)? array = null;

            // First compute the iteration range
            Value startValue;
            Value stepValue;
            Value endValue;
            if (stm.IterationValues.Expression is ResolvedExpression.RangeLiteral rlit)
            {
                // Item2 is always the end. Either Item1 is the start and 1 is the step,
                // or Item1 is a range expression itself, with Item1 the start and Item2 the step.
                this.Transformation.Expressions.OnTypedExpression(rlit.Item2);
                endValue = PopAndRegister(endName);

                if (rlit.Item1.Expression is ResolvedExpression.RangeLiteral rlitInner)
                {
                    // Item2 is now the step
                    this.Transformation.Expressions.OnTypedExpression(rlitInner.Item2);
                    stepValue = PopAndRegister(stepName);
                    // And Item1 the start
                    this.Transformation.Expressions.OnTypedExpression(rlitInner.Item1);
                    startValue = PopAndRegister(startName);
                }
                else
                {
                    // 1 is the step
                    stepValue = this.SharedState.Context.CreateConstant(1L);
                    this.SharedState.RegisterName(stepName, stepValue);
                    // And the original Item1 is the start
                    this.Transformation.Expressions.OnTypedExpression(rlit.Item1);
                    startValue = PopAndRegister(startName);
                }
            }
            else if (stm.IterationValues.ResolvedType.Resolution.IsRange)
            {
                this.Transformation.Expressions.OnTypedExpression(stm.IterationValues);
                var rangeValue = this.SharedState.ValueStack.Pop();
                startValue = this.SharedState.CurrentBuilder.ExtractValue(rangeValue, 0);
                stepValue = this.SharedState.CurrentBuilder.ExtractValue(rangeValue, 1);
                endValue = this.SharedState.CurrentBuilder.ExtractValue(rangeValue, 2);
                this.SharedState.RegisterName(startName, startValue);
                this.SharedState.RegisterName(stepName, stepValue);
                this.SharedState.RegisterName(endName, endValue);
            }
            else if (stm.IterationValues.ResolvedType.Resolution is QsResolvedTypeKind.ArrayType arrType)
            {
                var elementType = this.SharedState.LlvmTypeFromQsharpType(arrType.Item);
                startValue = this.SharedState.Context.CreateConstant(0L);
                stepValue = this.SharedState.Context.CreateConstant(1L);
                this.Transformation.Expressions.OnTypedExpression(stm.IterationValues);
                array = (this.SharedState.ValueStack.Pop(), elementType);
                var arrayLength = this.SharedState.CurrentBuilder.Call(
                    this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetLength),
                    array.Value.Item1,
                    this.SharedState.Context.CreateConstant(0));
                endValue = this.SharedState.CurrentBuilder.Sub(
                    arrayLength, this.SharedState.Context.CreateConstant(1L));
                this.SharedState.RegisterName(startName, startValue);
                this.SharedState.RegisterName(stepName, stepValue);
                this.SharedState.RegisterName(endName, endValue);
            }
            else
            {
                throw new InvalidOperationException("For loop through invalid value");
            }

            // If we're iterating through a range, we can use the iteration variable name directly.
            // Otherwise, we need to generate a unique name for the iteration through the array's indices.
            var iterationVar = array == null && stm.LoopItem.Item1 is SymbolTuple.VariableName loopVar
                ? loopVar.Item
                : this.SharedState.GenerateUniqueName("iter");

            // We need to reflect the standard LLVM block structure for a loop
            var preheaderName = this.SharedState.GenerateUniqueName("preheader");
            var headerName = this.SharedState.GenerateUniqueName("header");
            var bodyName = this.SharedState.GenerateUniqueName("body");
            var exitingName = this.SharedState.GenerateUniqueName("exiting");
            var exitName = this.SharedState.GenerateUniqueName("exit");

            var preheaderBlock = this.SharedState.CurrentFunction.AppendBasicBlock(preheaderName);
            var headerBlock = this.SharedState.CurrentFunction.AppendBasicBlock(headerName);
            var bodyBlock = this.SharedState.CurrentFunction.AppendBasicBlock(bodyName);
            var exitingBlock = this.SharedState.CurrentFunction.AppendBasicBlock(exitingName);
            var exitBlock = this.SharedState.CurrentFunction.AppendBasicBlock(exitName);

            // End the current block by branching to the preheader
            this.SharedState.CurrentBuilder.Branch(preheaderBlock);

            // Start a new naming scope
            this.SharedState.OpenNamingScope();

            // Preheader block: compute the range and test direction for the loop, then branch to the header
            this.SharedState.SetCurrentBlock(preheaderBlock);
            var testValue = this.SharedState.CurrentBuilder.Compare(
                IntPredicate.SignedGreaterThan,
                stepValue,
                this.SharedState.Context.CreateConstant(0L));
            this.SharedState.RegisterName(testName, testValue);
            this.SharedState.CurrentBuilder.Branch(headerBlock);

            // Header block: phi node to assign the iteration variable, then test
            this.SharedState.SetCurrentBlock(headerBlock);
            var iterationValue = this.SharedState.CurrentBuilder.PhiNode(this.SharedState.Types.Int);
            iterationValue.AddIncoming(startValue, preheaderBlock);
            this.SharedState.RegisterName(iterationVar, iterationValue);
            // We can't add the other incoming value yet, because we haven't generated it yet.
            // We'll add it when we generate the exiting block.
            // TODO: simplify the following if the step is a compile-time constant
            var aboveEnd = this.SharedState.CurrentBuilder.Compare(
                IntPredicate.SignedGreaterThanOrEqual, iterationValue, endValue);
            var belowEnd = this.SharedState.CurrentBuilder.Compare(
                IntPredicate.SignedLessThanOrEqual, iterationValue, endValue);
            var continueValue = this.SharedState.CurrentBuilder.Select(testValue, belowEnd, aboveEnd);
            this.SharedState.CurrentBuilder.Branch(continueValue, bodyBlock, exitBlock);

            // Body block -- first, if we're stepping through an array, we need to fetch the array element
            // and potentially deconstruct it
            this.SharedState.SetCurrentBlock(bodyBlock);
            this.SharedState.ScopeMgr.OpenScope();
            if (array != null)
            {
                var p = this.SharedState.CurrentBuilder.Call(
                    this.SharedState.GetOrCreateRuntimeFunction(RuntimeLibrary.ArrayGetElementPtr1d), array.Value.Item1, iterationValue);
                var itemPtr = this.SharedState.CurrentBuilder.BitCast(p, array.Value.Item2.CreatePointerType());
                var item = this.SharedState.CurrentBuilder.Load(array.Value.Item2, itemPtr);
                this.BindSymbolTuple(stm.LoopItem.Item1, item, stm.LoopItem.Item2, true);
            }

            // Now finish the block with the statements in the body
            this.Transformation.Statements.OnScope(stm.Body);
            var isTerminated = this.SharedState.CurrentBlock?.Terminator != null;
            this.SharedState.ScopeMgr.CloseScope(isTerminated);
            if (!isTerminated)
            {
                this.SharedState.CurrentBuilder.Branch(exitingBlock);
            }

            // Exiting block -- update the iteration value and the phi node
            this.SharedState.SetCurrentBlock(exitingBlock);
            var nextValue = this.SharedState.CurrentBuilder.Add(iterationValue, stepValue);
            iterationValue.AddIncoming(nextValue, exitingBlock);
            this.SharedState.CurrentBuilder.Branch(headerBlock);

            // And finally, the exit block -- empty to start with
            this.SharedState.SetCurrentBlock(exitBlock);

            // and close the naming scope
            this.SharedState.CloseNamingScope();

            return QsStatementKind.EmptyStatement;
        }

        /// <exception cref="InvalidOperationException">The current function is set to null.</exception>
        public override QsStatementKind OnRepeatStatement(QsRepeatStatement stm)
        {
            if (this.SharedState.CurrentFunction == null)
            {
                throw new InvalidOperationException("current function is set to null");
            }

            // The basic approach here is to put the repeat into one basic block.
            // A second basic block holds the evaluation of the test expression and the test itself.
            // The fixup is in a third basic block, and then there is a final basic block as the continuation.
            // We could merge the repeat and the test into one block, but it seems that it might be easier to
            // analyze the loop later if we do it this way.
            // We need to be a bit careful about scopes here, though.

            this.SharedState.OpenNamingScope();

            var repeatBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("repeat"));
            var testBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("until"));
            var fixupBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("fixup"));
            var contBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("rend"));

            this.SharedState.CurrentBuilder.Branch(repeatBlock);

            this.SharedState.ScopeMgr.OpenScope();
            this.ProcessUnscopedBlock(repeatBlock, stm.RepeatBlock.Body, testBlock);

            this.SharedState.SetCurrentBlock(testBlock);
            this.Transformation.Expressions.OnTypedExpression(stm.SuccessCondition);
            var test = this.SharedState.ValueStack.Pop();
            this.SharedState.CurrentBuilder.Branch(test, contBlock, fixupBlock);

            this.ProcessUnscopedBlock(fixupBlock, stm.FixupBlock.Body, repeatBlock);

            this.SharedState.SetCurrentBlock(contBlock);
            this.SharedState.ScopeMgr.CloseScope(this.SharedState.CurrentBlock?.Terminator != null);

            this.SharedState.CloseNamingScope();

            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnReturnStatement(TypedExpression ex)
        {
            // If we're not inlining, compute the result, release any pending qubits, and generate a return.
            // Otherwise, just evaluate the result and leave it on top of the stack.
            if (this.SharedState.CurrentInlineLevel == 0)
            {
                this.Transformation.Expressions.OnTypedExpression(ex);
                Value result = this.SharedState.ValueStack.Pop();

                // Make sure not to unreference the return value
                this.SharedState.ScopeMgr.RemovePendingValue(result);

                // Release any locally-allocated qubits and dereference allocated values
                this.SharedState.ScopeMgr.ExitScope();

                if (ex.ResolvedType.Resolution.IsUnitType)
                {
                    this.SharedState.CurrentBuilder.Return();
                }
                else
                {
                    this.SharedState.CurrentBuilder.Return(result);
                }
            }
            else
            {
                this.Transformation.Expressions.OnTypedExpression(ex);
            }

            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnValueUpdate(QsValueUpdate stm)
        {
            // Given a symbol with an existing binding, update the binding to a bew value and
            // addref the new value (if it's a ref-counted type).
            // The old value will get released when the scope is closed or exited.
            void UpdateBinding(string symbol, Value newValue)
            {
                var ptr = this.SharedState.GetNamedPointer(symbol);
                this.SharedState.CurrentBuilder.Store(newValue, ptr);
                this.SharedState.AddReference(newValue);
            }

            // Update a tuple of items from a tuple value.
            void UpdateTuple(ImmutableArray<TypedExpression> items, Value val)
            {
                var itemTypes = items.Select(i => this.SharedState.LlvmTypeFromQsharpType(i.ResolvedType)).ToArray();
                var tupleType = this.SharedState.Types.CreateConcreteTupleType(itemTypes);
                var tuplePointer = this.SharedState.CurrentBuilder.BitCast(val, tupleType.CreatePointerType());
                for (int i = 0; i < items.Length; i++)
                {
                    var itemValuePtr = this.SharedState.GetTupleElementPointer(tupleType, tuplePointer, i + 1);
                    var itemValue = this.SharedState.CurrentBuilder.Load(itemTypes[i], itemValuePtr);
                    UpdateItem(items[i], itemValue);
                }
            }

            // Update an item, which might be a single symbol or a tuple, to a new Value
            void UpdateItem(TypedExpression item, Value itemValue)
            {
                if (item.Expression is ResolvedExpression.Identifier id && id.Item1 is Identifier.LocalVariable varName)
                {
                    UpdateBinding(varName.Item, itemValue);
                }
                else if (item.Expression is ResolvedExpression.ValueTuple tuple)
                {
                    UpdateTuple(tuple.Item, itemValue);
                }
                else
                {
                    // This should never happen
                    throw new InvalidOperationException("set statement with invalid left-hand side");
                }
            }

            this.Transformation.Expressions.OnTypedExpression(stm.Rhs);
            var value = this.SharedState.ValueStack.Pop();
            UpdateItem(stm.Lhs, value);
            return QsStatementKind.EmptyStatement;
        }

        public override QsStatementKind OnVariableDeclaration(QsBinding<TypedExpression> stm)
        {
            this.Transformation.Expressions.OnTypedExpression(stm.Rhs);
            var val = this.SharedState.ValueStack.Pop();
            this.BindSymbolTuple(stm.Lhs, val, stm.Rhs.ResolvedType, stm.Kind.IsImmutableBinding);

            return QsStatementKind.EmptyStatement;
        }

        /// <exception cref="InvalidOperationException">The current function is set to null.</exception>
        public override QsStatementKind OnWhileStatement(QsWhileStatement stm)
        {
            if (this.SharedState.CurrentFunction == null)
            {
                throw new InvalidOperationException("current function is set to null");
            }

            // The basic approach here is to put the evaluation of the test expression into one basic block,
            // the body of the loop in a second basic block, and then have a third basic block as the continuation.

            var testBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("while"));
            var bodyBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("do"));
            var contBlock = this.SharedState.CurrentFunction.AppendBasicBlock(this.SharedState.GenerateUniqueName("wend"));

            this.SharedState.CurrentBuilder.Branch(testBlock);
            this.SharedState.SetCurrentBlock(testBlock);
            // The OpenScope is almost certainly unnecessary, but it is technically possible for the condition
            // expression to perform an allocation that needs to get cleaned up, so...
            this.SharedState.ScopeMgr.OpenScope();
            this.Transformation.Expressions.OnTypedExpression(stm.Condition);
            var test = this.SharedState.ValueStack.Pop();
            this.SharedState.ScopeMgr.CloseScope(this.SharedState.CurrentBlock?.Terminator != null);
            this.SharedState.CurrentBuilder.Branch(test, bodyBlock, contBlock);

            this.SharedState.OpenNamingScope();
            this.ProcessBlock(bodyBlock, stm.Body, testBlock);
            this.SharedState.CloseNamingScope();
            this.SharedState.SetCurrentBlock(contBlock);
            return QsStatementKind.EmptyStatement;
        }
    }
}
