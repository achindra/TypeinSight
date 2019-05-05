using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeinSight
{
    public class TypingData
    {
        public uint KeyCode;
        public uint KeyTime;
    }

    public class TypedData
    {
        public string User;
        public string Machine;
        public string strData;
        public uint   strTime;
        public uint   grams;

        public TypedData()
        {
            this.User = "";
            this.Machine = "";
            this.strData = "";
            this.strTime = 0;
            this.grams = 0;

        }
    }

    public class TypedWord
    {
        private TypingData[] _typingData;
        private uint counter = 0;
        private const uint arrayLenght = 32;

        public TypedWord()
        {
            _typingData = new TypingData[arrayLenght];
            for (ulong i = 0; i < arrayLenght; i++)
            {
                _typingData[i] = new TypingData();
                _typingData[i].KeyCode = 0;
                _typingData[i].KeyTime = 0;
            }
        }

        public TypedData GetTypedData(uint keyCode, uint keyTime)
        {
            TypedData typedData = null;

            char key = Convert.ToChar(keyCode);

            if (char.IsLetterOrDigit(key))
            {
                _typingData[counter].KeyCode = keyCode;
                _typingData[counter].KeyTime = keyTime;
                counter++;
            }

            if(!char.IsLetterOrDigit(key) || counter >= arrayLenght)
            {
                typedData = new TypedData();
                char[] _text = new char[arrayLenght+1];

                for (ulong i = 0; i < arrayLenght; i++)
                {
                    _text[i] = Convert.ToChar(_typingData[i].KeyCode);
                   typedData.strTime += _typingData[i].KeyTime;

                    _typingData[i].KeyCode = Convert.ToChar(0);
                    _typingData[i].KeyTime = 0;
                }
                _text[arrayLenght-1] = Convert.ToChar(0);
                typedData.strData = new string(_text);
                typedData.grams = 0;
                typedData.User = Environment.UserName;
                typedData.Machine = Environment.MachineName;
                counter = 0;
            }


            return typedData;
        }
    }
}
