/* The MIT License (MIT)
 * 
 * Copyright (c) 2013 PrimeJSON
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to 
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace PrimeJSON.Core
{
    public class JSONData : JSONNode
    {
        private string? _sValue;
        private bool _bValue;
        private double _dValue;
        private string _type = "JSONData";

        public JSONData(string? value)
        {
            this._sValue = value;
            if (value == null)
                this._type = "JSONNull";
            else
                this._type = "JSONString";
        }

        public JSONData(bool value)
        {
            this._bValue = value;
            this._type = "JSONBoolean";
        }

        public JSONData(double value)
        {
            ValidateNumber(value);
            this._dValue = value;
            this._type = "JSONNumber";
        }

        public override string? JSONString
        {
            get
            {
                return this._sValue;
            }
            set
            {
                this._sValue = value;
                if (this._sValue == null)
                    this._type = "JSONNull";
                else
                    this._type = "JSONString";
            }
        }

        public override bool JSONBoolean
        {
            get
            {
                return this._bValue;
            }
            set
            {
                this._bValue = value;
                this._type = "JSONBoolean";
            }
        }

        public override double JSONNumber
        {
            get
            {
                return this._dValue;
            }
            set
            {
                ValidateNumber(value);
                this._dValue = value;
                this._type = "JSONNumber";
            }
        }

        public override string Type
        {
            get
            {
                return this._type;
            }
        }

        private static void ValidateNumber(double value)
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value), "JSON numbers must be finite.");
        }

        public override string ToJSON() => JSONSerialization.Serialize(this);
    }
}
