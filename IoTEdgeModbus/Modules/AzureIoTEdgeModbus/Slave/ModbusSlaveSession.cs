﻿namespace AzureIoTEdgeModbus.Slave
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Base class of Modbus session.
    /// </summary>
    public abstract class ModbusSlaveSession
    {
        public static int ModbusExceptionCode = 0x80;

        public ModbusSlaveConfig config;
        protected object OutMessage = null;
        protected const int m_bufSize = 512;
        protected SemaphoreSlim m_semaphore_collection = new SemaphoreSlim(1, 1);
        protected SemaphoreSlim m_semaphore_connection = new SemaphoreSlim(1, 1);
        protected bool m_run = false;
        protected List<Task> m_taskList = new List<Task>();
        protected virtual int m_reqSize { get; }
        protected virtual int m_dataBodyOffset { get; }
        protected virtual int m_silent { get; }

        #region Constructors
        public ModbusSlaveSession(ModbusSlaveConfig conf)
        {
            this.config = conf;
        }
        #endregion

        #region Public Methods
        public abstract void ReleaseSession();

        public async Task InitSession()
        {
            await this.ConnectSlave();

            foreach (var op_pair in this.config.Operations)
            {
                ReadOperation x = op_pair.Value;

                x.RequestLen = this.m_reqSize;
                x.Request = new byte[m_bufSize];

                this.EncodeRead(x);
            }
        }

        public async Task WriteCB(string uid, ReadOperation readOperation, string value)
        {
            byte[] writeRequest = new byte[m_bufSize];
            byte[] writeResponse = null;
            int reqLen = this.m_reqSize;

            this.EncodeWrite(writeRequest, uid, readOperation, value);

            writeResponse = await this.SendRequest(writeRequest, reqLen);
        }
        public void ProcessOperations()
        {
            this.m_run = true;
            foreach (var op_pair in this.config.Operations)
            {
                ReadOperation x = op_pair.Value;
                Task t = Task.Run(async () => await this.SingleOperation(x));
                this.m_taskList.Add(t);
            }
        }
        public object GetOutMessage()
        {
            return this.OutMessage;
        }
        public void ClearOutMessage()
        {
            this.m_semaphore_collection.Wait();

            this.OutMessage = null;

            this.m_semaphore_collection.Release();
        }
        #endregion

        #region Protected Methods
        protected abstract void EncodeWrite(byte[] request, string uid, ReadOperation readOperation, string value);
        protected abstract Task<byte[]> SendRequest(byte[] request, int reqLen);
        protected abstract Task ConnectSlave();
        protected abstract void EncodeRead(ReadOperation operation);
        protected async Task SingleOperation(ReadOperation x)
        {
            while (this.m_run)
            {
                x.Response = null;
                x.Response = await this.SendRequest(x.Request, x.RequestLen);

                if (x.Response != null)
                {
                    if (x.Request[this.m_dataBodyOffset] == x.Response[this.m_dataBodyOffset])
                    {
                        this.ProcessResponse(this.config, x);
                    }
                    else if (x.Request[this.m_dataBodyOffset] + ModbusExceptionCode == x.Response[this.m_dataBodyOffset])
                    {
                        Console.WriteLine($"Modbus exception code: {x.Response[this.m_dataBodyOffset + 1]}");
                    }
                }
                await Task.Delay(x.PollingInterval - this.m_silent);
            }
        }
        protected void ProcessResponse(ModbusSlaveConfig config, ReadOperation x)
        {
            int count = 0;
            int step_size = 0;
            int start_digit = 0;
            List<ModbusOutValue> value_list = new List<ModbusOutValue>();
            switch (x.Response[this.m_dataBodyOffset])//function code
            {
                case (byte)FunctionCodeType.ReadCoils:
                case (byte)FunctionCodeType.ReadInputs:
                    {
                        count = x.Response[this.m_dataBodyOffset + 1] * 8;
                        count = (count > x.Count) ? x.Count : count;
                        step_size = 1;
                        start_digit = x.Response[this.m_dataBodyOffset] - 1;
                        break;
                    }
                case (byte)FunctionCodeType.ReadHoldingRegisters:
                case (byte)FunctionCodeType.ReadInputRegisters:
                    {
                        count = x.Response[this.m_dataBodyOffset + 1];
                        step_size = 2;
                        start_digit = (x.Response[this.m_dataBodyOffset] == 3) ? 4 : 3;
                        break;
                    }
            }
            for (int i = 0; i < count; i += step_size)
            {
                string res = "";
                string cell = "";
                string val = "";
                if (step_size == 1)
                {
                    cell = string.Format(x.OutFormat, (char)x.Entity, x.Address + i + 1);
                    val = string.Format("{0}", (x.Response[this.m_dataBodyOffset + 2 + (i / 8)] >> (i % 8)) & 0b1);
                }
                else if (step_size == 2)
                {
                    cell = string.Format(x.OutFormat, (char)x.Entity, x.Address + (i / 2) + 1);
                    val = string.Format("{0,00000}", ((x.Response[this.m_dataBodyOffset + 2 + i]) * 0x100 + x.Response[this.m_dataBodyOffset + 3 + i]));
                }
                res = cell + ": " + val + "\n";
                Console.WriteLine(res);

                ModbusOutValue value = new ModbusOutValue()
                { DisplayName = x.DisplayName, Address = cell, Value = val };
                value_list.Add(value);
            }

            if (value_list.Count > 0)
            {
                this.PrepareOutMessage(config.HwId, x.CorrelationId, value_list);
            }
        }
        protected void PrepareOutMessage(string HwId, string CorrelationId, List<ModbusOutValue> ValueList)
        {
            this.m_semaphore_collection.Wait();
            ModbusOutContent content = null;
            if (this.OutMessage == null)
            {
                content = new ModbusOutContent
                {
                    HwId = HwId,
                    Data = new List<ModbusOutData>()
                };
                this.OutMessage = content;
            }
            else
            {
                content = (ModbusOutContent)this.OutMessage;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ModbusOutData data = null;
            foreach (var d in content.Data)
            {
                if (d.CorrelationId == CorrelationId && d.SourceTimestamp == timestamp)
                {
                    data = d;
                    break;
                }
            }
            if (data == null)
            {
                data = new ModbusOutData
                {
                    CorrelationId = CorrelationId,
                    SourceTimestamp = timestamp,
                    Values = new List<ModbusOutValue>()
                };
                content.Data.Add(data);
            }

            data.Values.AddRange(ValueList);

            this.m_semaphore_collection.Release();

        }
        protected void ReleaseOperations()
        {
            this.m_run = false;
            Task.WaitAll(this.m_taskList.ToArray());
            this.m_taskList.Clear();
        }
        #endregion
    }
}