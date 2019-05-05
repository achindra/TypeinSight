using System;

namespace TypeinSight
{
   public class nGrams
   {

      private TypingData[] _nGramData;
      private ulong counter;
      private uint grams;

      public nGrams(uint _grams)
      {
         grams = _grams;
         _nGramData = new TypingData[grams];
         counter = grams;

         for (uint i = 0; i < grams; i++)
         {
            _nGramData[i] = new TypingData();
         }
      }

      public TypedData GetTypedData(uint keyCode, uint keyTime)
      {
         TypedData typedData = new TypedData();

         _nGramData[counter % grams].KeyCode = keyCode;
         _nGramData[counter % grams].KeyTime = keyTime;

         char[] _text = new char[grams + 1];

         for (uint i = 0; i < grams; i++)
         {
            _text[i] = Convert.ToChar(_nGramData[(counter - i) % grams].KeyCode);
            typedData.strTime += _nGramData[(counter - i) % grams].KeyTime;
         }
         _text[grams] = Convert.ToChar(0);
         typedData.strData = new string(_text);
         typedData.grams = grams;
         typedData.User = Environment.UserName;
         typedData.Machine = Environment.MachineName;
         counter++;

         return typedData;
      }
   }
}
