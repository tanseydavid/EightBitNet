﻿// <copyright file="Processor.cs" company="Adrian Conlon">
// Copyright (c) Adrian Conlon. All rights reserved.
// </copyright>

namespace EightBit
{
    using System;

    public abstract class Processor : ClockedChip
    {
        private PinLevel resetLine;
        private PinLevel intLine;

        protected Processor(Bus memory) => this.Bus = memory;

        public event EventHandler<EventArgs> RaisingRESET;

        public event EventHandler<EventArgs> RaisedRESET;

        public event EventHandler<EventArgs> LoweringRESET;

        public event EventHandler<EventArgs> LoweredRESET;

        public event EventHandler<EventArgs> RaisingINT;

        public event EventHandler<EventArgs> RaisedINT;

        public event EventHandler<EventArgs> LoweringINT;

        public event EventHandler<EventArgs> LoweredINT;

        public ref PinLevel RESET => ref this.resetLine;

        public ref PinLevel INT => ref this.intLine;

        public Bus Bus { get; }

        public Register16 PC { get; } = new Register16();

        protected byte OpCode { get; set; }

        // http://graphics.stanford.edu/~seander/bithacks.html#FixedSignExtend
        public static sbyte SignExtend(int b, byte x)
        {
            var m = (byte)(1 << (b - 1)); // mask can be pre-computed if b is fixed
            x = (byte)(x & ((1 << b) - 1));  // (Skip this if bits in x above position b are already zero.)
            var result = (x ^ m) - m;
            return (sbyte)result;
        }

        public static sbyte SignExtend(int b, int x) => SignExtend(b, (byte)x);

        public abstract int Step();

        public abstract int Execute();

        public int Run(int limit)
        {
            var current = 0;
            while (this.Powered && (current < limit))
            {
                current += this.Step();
            }

            return current;
        }

        public int Execute(byte value)
        {
            this.OpCode = value;
            return this.Execute();
        }

        public abstract Register16 PeekWord(ushort address);

        public abstract void PokeWord(ushort address, Register16 value);

        public void PokeWord(ushort address, ushort value) => this.PokeWord(address, new Register16(value));

        public virtual void RaiseRESET()
        {
            this.OnRaisingRESET();
            this.RESET.Raise();
            this.OnRaisedRESET();
        }

        public virtual void LowerRESET()
        {
            this.OnLoweringRESET();
            this.RESET.Lower();
            this.OnLoweredRESET();
        }

        public virtual void RaiseINT()
        {
            this.OnRaisingINT();
            this.INT.Raise();
            this.OnRaisedINT();
        }

        public virtual void LowerINT()
        {
            this.OnLoweringINT();
            this.INT.Lower();
            this.OnLoweredINT();
        }

        protected virtual void OnRaisingRESET() => this.RaisingRESET?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedRESET() => this.RaisedRESET?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringRESET() => this.LoweringRESET?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredRESET() => this.LoweredRESET?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisingINT() => this.RaisingINT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnRaisedINT() => this.RaisedINT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweringINT() => this.LoweringINT?.Invoke(this, EventArgs.Empty);

        protected virtual void OnLoweredINT() => this.LoweredINT?.Invoke(this, EventArgs.Empty);

        protected virtual void HandleRESET() => this.RaiseRESET();

        protected virtual void HandleINT() => this.RaiseINT();

        protected void BusWrite(byte low, byte high, byte data)
        {
            this.Bus.Address.Low = low;
            this.Bus.Address.High = high;
            this.BusWrite(data);
        }

        protected void BusWrite(ushort address, byte data)
        {
            this.Bus.Address.Word = address;
            this.BusWrite(data);
        }

        protected void BusWrite(Register16 address, byte data) => this.BusWrite(address.Word, data);

        protected void BusWrite(byte data)
        {
            this.Bus.Data = data;
            this.BusWrite();
        }

        protected virtual void BusWrite() => this.Bus.Write();   // N.B. Should be the only real call into the "Bus.Write" code.

        protected byte BusRead(byte low, byte high)
        {
            this.Bus.Address.Low = low;
            this.Bus.Address.High = high;
            return this.BusRead();
        }

        protected byte BusRead(ushort address)
        {
            this.Bus.Address.Word = address;
            return this.BusRead();
        }

        protected byte BusRead(Register16 address) => this.BusRead(address.Word);

        protected virtual byte BusRead() => this.Bus.Read();   // N.B. Should be the only real call into the "Bus.Read" code.

        protected byte FetchByte() => this.BusRead(this.PC.Word++);

        protected abstract Register16 GetWord();

        protected abstract void SetWord(Register16 value);

        protected abstract Register16 GetWordPaged(byte page, byte offset);

        protected abstract void SetWordPaged(byte page, byte offset, Register16 value);

        protected abstract Register16 FetchWord();

        protected abstract void Push(byte value);

        protected abstract byte Pop();

        protected abstract void PushWord(Register16 value);

        protected abstract Register16 PopWord();

        protected Register16 GetWord(ushort address)
        {
            this.Bus.Address.Word = address;
            return this.GetWord();
        }

        protected Register16 GetWord(Register16 address) => this.GetWord(address.Word);

        protected void SetWord(ushort address, Register16 value)
        {
            this.Bus.Address.Word = address;
            this.SetWord(value);
        }

        protected void SetWord(Register16 address, Register16 value) => this.SetWord(address.Word, value);

        protected void Jump(ushort destination) => this.PC.Word = destination;

        protected void Call(ushort destination)
        {
            this.PushWord(this.PC);
            this.Jump(destination);
        }

        protected virtual void Return() => this.Jump(this.PopWord().Word);
    }
}
