using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LoadModules.Specific.HIC.Microbiology
{

    public interface IMicrobiologyResultRecord
    {
        
    }

    internal class MicrobiologyHelper
    {
        public static string[] SplitByWhitespace(string currentLine)
        {
            return currentLine.Trim().Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string GetValueFromWhitespaceSeperatedLine(string currentLine, int index)
        {
            return currentLine.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[index];
        }
    }

    public class MB_Tests : IMicrobiologyResultRecord
   {
      public string SpecimenNo { get; set; }
      public string TestCode { get; set; }
      public string ResultCode { get; set; }

      public MB_Tests(MB_Lab parent, string fromLine)
        {
            string[] data = MicrobiologyHelper.SplitByWhitespace(fromLine);


            //line should look something like 
            //BCOM                    P
            //but could equally be
            //NBC                     GPC ( ? STAPH )
            //notice the spam of spaces in NBC result, that is why we have to aggregate
            
            
          SpecimenNo = parent.SpecimenNo;
          TestCode = data[0];//first array element enters as the Test Code

          //apparently also we can get (once in every 100,000?, so in cases like this leave resultcode as null)
          //CDNR

          if (data.Length >= 2)
              ResultCode = data.Skip(1).Aggregate("", (s, n) => s + " " + n).Trim(); ; //rest enters as result code

        }

   }

    public class MB_Lab : IMicrobiologyResultRecord
   {
      public string SpecimenNo { get; set; }
      public string CHI { get; set; }
      public string surname { get; set; }
      public DateTime? DoB { get; set; }
      public string Sex { get; set; }
      public DateTime? SampleDate { get; set; }
      public string SampleTime { get; set; }
      public string SpecimenType { get; set; }
      public DateTime? RcvDate { get; set; }
      public string RcvTime { get; set; }
      public string Source { get; set; }
      public string Dept { get; set; }
      public string Clinician { get; set; }
      public string Comments { get; set; }
   }

    public class MB_NoIsolations : IMicrobiologyResultRecord
   {
        public string SpecimenNo { get; set; }
        public string ResultCode { get; set; }

        public MB_NoIsolations(MB_Lab parent, string currentLine)
        {

            SpecimenNo = parent.SpecimenNo;
            ResultCode = MicrobiologyHelper.GetValueFromWhitespaceSeperatedLine(currentLine,1);
        }
   }


    public class MB_IsolationsCollection
    {

        //basic facts -- will be the same for all Isolations spawned by this collection:
        private string _specimenNo;
        private string _organismCode;
        private string _weightGrowth_cd;
        private string _intCode1;
        private string _intCode2;

        /// <summary>
        /// Create one of these every time you see a field like SMAR|P|Y|Y then remember that you created it because you need to update it with data
        /// from 2 SENS blocks in the file, see AddBlockAString and SpawnWithBlockBString for details
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="initialLine"></param>
        public MB_IsolationsCollection(MB_Lab parent, string initialLine)
        {
            //expects line to look something like:
            //$CULT                   SMAR|P|Y|Y

            if(!initialLine.StartsWith("$CULT"))
                throw new Exception("Line must start with $CULT to be a new IsolationCollection");

           //basic facts
            string resultData = MicrobiologyHelper.GetValueFromWhitespaceSeperatedLine(initialLine, 1);
            string[] fields = resultData.Split(new[] {'|'}).ToArray();

            if(fields.Length != 4)
                throw new Exception("Expected Isolations to contain 4 pipes, this contained " + fields.Length + " pipes (if 0 pipes then it should be a NoIsolations instead of an isolations collection)");

            _specimenNo = parent.SpecimenNo;
            _organismCode = fields[0];
            _weightGrowth_cd = fields[1];
            _intCode1 = fields[2];
            _intCode2 = fields[3];
        }

        
        List<string> BlockAStrings = new List<string>();
        private int spawnCounter = 0;

        /// <summary>
        /// Input file comes in as 
        /// 
        ///$CULT                   SMAR|P|Y|Y
        ///$CULT                   PAER|P|Y|Y
        ///Followed by one $SENS result block A and one $SENS result block B
        /// Block A is filled with 3 letter codes e.g. AUG, CFM etc
        /// Block B has the results either 'r', 'S', 's', '-' etc
        /// 
        /// For each BlockA call this method
        /// For each BlockB (must be the same number of lines) call SpawnWithBlockBString which will release a finalised Isolations record
        /// 
        ///  </summary>
        /// <param name="currentLine"></param>
        public void AddBlockAString(string currentLine)
        {
            if (currentLine.StartsWith("$SENS"))
                currentLine = currentLine.Substring("$SENS".Length);

            BlockAStrings.Add(currentLine.Trim());
        }

        public MB_IsolationResult SpawnResultWithBlockBString( string currentLine)
        {
            if(spawnCounter + 1 > BlockAStrings.Count)
                if (currentLine.Equals("                        -"))//we have overrun the headers blocks but its ok because this is a dash anyway... probably like thier equivellent of an 'ignore me line'
                    return null;
                else
                    throw new Exception("Cannot spawn results because not enough BlockA strings were passed to collection (is there a difference between the number of SENS results in Block A and the number of SENS results in Block B?");

            if (currentLine.StartsWith("$SENS"))
                currentLine = currentLine.Substring("$SENS".Length);
            
            currentLine = currentLine.Trim();
            try
            {
                return new MB_IsolationResult()
                {
                    SpecimenNo = _specimenNo,
                    organismCode = _organismCode,
                    AB_cd = BlockAStrings[spawnCounter],
                    AB_result = currentLine
                };
            }
            finally
            {
                spawnCounter++;
            }
        }

        public IMicrobiologyResultRecord SpawnIsolation()
        {
            return new MB_Isolation()
            {
                SpecimenNo = _specimenNo,
                organismCode = _organismCode,
                WeightGrowth_cd = _weightGrowth_cd,
                IntCode1 = _intCode1,
                IntCode2 = _intCode2

            };
        }
    }
   public class MB_Isolation : IMicrobiologyResultRecord
   {
      public string SpecimenNo{ get; set; }
      public string organismCode{ get; set; }
      public string WeightGrowth_cd{ get; set; }
      public string IntCode1{ get; set; }
      public string IntCode2{ get; set; }
   }

    public class MB_IsolationResult : IMicrobiologyResultRecord
    {
        public string SpecimenNo { get; set; }
        public string organismCode { get; set; }
        public string AB_cd { get; set; }
        public string AB_result { get; set; }
    }

   public class MicroBiologyFileReader
   {

       private TextReader _textReader = null;
       
       
      /// <summary>
       /// file name being currently loaded, can be accessed during enumeration of ProcessFile to find out more about the error location
      /// </summary>
       public string FileName { get; private set; }
       /// <summary>
       /// current line number of the file 'FileName' being currently loaded, can be accessed during enumeration of ProcessFile to find out more about the error location
       /// </summary>
       public int LineNumber { get; private set; }

      string specimen;
       public event WarningHandler Warning;

       public MicroBiologyFileReader(string fileName)
      {
          FileName = fileName;
          _textReader = new StreamReader(fileName);
      }

       public MicroBiologyFileReader(TextReader tr)
       {
           FileName = "Direct From Stream";
           _textReader = tr;
       }
      public IEnumerable<IMicrobiologyResultRecord> ProcessFile()
      {
          if(Warning == null)
              throw new NullReferenceException("Nobody is listening to warnings");

          bool end_of_tests;

          specimen = GetSpecimenNo(_textReader);

          for (; ; )
          {
              MB_Lab lab = new MB_Lab();
              end_of_tests = false;
              lab.SpecimenNo = specimen; // specimen no. is read at end of previous record

              lab.CHI = TrimOrNullify(ReadLine());
              lab.surname = TrimOrNullify(ReadLine());

              string date = ReadLine();
              lab.DoB = DateOrNull(date);
              lab.Sex = TrimOrNullify(ReadLine());

              // tests coming up now
              List<MB_IsolationsCollection> isolationCollections = new List<MB_IsolationsCollection>();


              string testComments = "";

              string currentLine = ReadLine();
              bool got_cult = false;
              while (!end_of_tests)
              {
                  if (string.IsNullOrWhiteSpace(currentLine))
                      break;
                  if (is_date(currentLine))
                      break;
                  if (currentLine[0] != '$' && got_cult)
                      break;

                  //its not got spaces in it so it is either a comment or a test code which has no result (since ones with results appear like 'GWBC                    FW')
                  if (!currentLine.Contains("            "))
                  {
                      //its something like BLAN or 'bob is fine and happy and dandy'
                      string randomCrud = currentLine.Trim();

                      //if it is short it's probably a test code (with no result)
                      if (randomCrud.Length <= 5)
                          yield return new MB_Tests(lab, randomCrud);
                      else
                          testComments += randomCrud + " "; //otherwise it's probably a comment <- scientific!
                      
                  }
                  else
                  //anything that doesn't start with a $ is a specimen
                  if (!currentLine.StartsWith("$"))
                  {
                      //sometimes they shove comments right in the middle of test results boo
                      if (currentLine.StartsWith("                        "))
                      {
                          if (!currentLine.EndsWith("-")) //sometimes those coments are nothing though i.e. "                        -"
                              testComments += currentLine.Trim()+ " ";
                      }
                      else
                          yield return new MB_Tests(lab,currentLine);
                  }
                  else
                  {

                      //BASIC CASE
                      //either:
                      //$CULT                   NHEC                            --NoIsolation
                      //or
                      //$CULT                   MANA|P|Y|Y                      --Isolation which means a culture with known sens blocks, which will appear further down file
                      //or 
                      //$SENS                   NYS                             --Sens Block A --matches the MANA above (you can tell it matches the MANA because it has pipes in it... yes that's how wacky this file is)
                      //                        MET
                      //$SENS                   r                               --Sens Block B1
                      //                        r
                      //$SENS                   r                               --Sens Block B2
                      //                        r

                      //FURTHER COMPLCATIONS
                      //so the correct way to interpret it is as follows

                      //if theres a $CULT and no pipes its a NoIsolations
                      //if theres a $CULT and pipes its an Isolations which means it will be followed by one or more:
                            //$SENS     Code1           <--- only ever 1 of these regardless of the number of  $CULT Bob|P|Y|N found above it
                            //          Code2
                            //$SENS     r               <--- 1 of these per $CULT Bob|P|Y|N found above it
                            //          s
                            //$SENS     r
                            //          s
                            //$SENS     r
                            //          s
                      //these all get parsed out and included as additional records all Isolations

                      //where the first is defined as noisolations and the second is defined as isolations

                      // deal with isolations/no isolations
                      if (currentLine.StartsWith("$CULT"))
                      {
                          got_cult = true;
                          if (!currentLine.Contains('|'))
                          {
                              //theoretically there are rows which are just $CULT then nothing in which case discard record completely.
                              if (!currentLine.Trim().Equals("$CULT"))
                                  // no isolations
                                  yield return new MB_NoIsolations(lab, currentLine);
                          }
                          else
                          {
                              MB_IsolationsCollection collection = new MB_IsolationsCollection(lab, currentLine);
                              isolationCollections.Add(collection);
                              yield return collection.SpawnIsolation();//yield the header e.g. MANA|P|Y|Y - but remember the collection for when it comes time to spew out all the results
                          }
                      }
                      else
                      {
                          if(!currentLine.StartsWith("$SENS"))
                              throw new Exception("Expected $SENS to follow after results");

                          bool areInBlockA = true;
                          int blockCounter = 0;
                          bool haveComplainedAboutBufferOverflow = false;
                          bool haveComplainedAboutSpawning = false;

                          //while we are still reading blocks -- terminates with a blank line or a date (part of lab record)
                          while (
                              !string.IsNullOrWhiteSpace(currentLine) //stop if we reach end of file
                              &&
                              !is_date(currentLine)//stop if we reach date
                              &&
                              currentLine.Contains("            ")//stop if we reach a comment (-something that doesn't contain a fistfull of spaces)
                              &&
                              (currentLine.StartsWith("$") || currentLine.StartsWith(" ") || currentLine.StartsWith("\t")) //stop if it is a comment (i.e. isn't "$SENS                        R" or "                        s" etc)
                              )
                          {
                              //deal with data on the current line
                              if (areInBlockA)
                              {
                                  //we recieved a bunch of $SENS without any corresponding $CULT e.g. "$CULT                   SAUR|P|Y|Y"
                                  if(isolationCollections.Count == 0)
                                  {
                                      if(!haveComplainedAboutBufferOverflow)
                                      {
                                          Warning(this,"Found $SENS block without accompanying '$CULT        X|Y|Y|Y' block will be ignored");
                                          haveComplainedAboutBufferOverflow = true;
                                      }
                                  }
                                  else
                                      foreach (var isolationCollection in isolationCollections)//we have a Block A  e.g. a fist full of isolation headers e.g "$SENS                   NYS" <-- we refer to this as a BlockA
                                          isolationCollection.AddBlockAString(currentLine);
                              }
                              else
                              {

                                  if (blockCounter >= isolationCollections.Count)
                                  {

                                      if (!haveComplainedAboutSpawning)
                                      {
                                          Warning(this,"Ignoring spawn result '" + currentLine +"' because not enough isolation collections were harvested earlier (count of isolation collections is:" + isolationCollections.Count +")");
                                          haveComplainedAboutSpawning = true;

                                      }
                                      
                                  }
                                  else
                                  {
                                      MB_IsolationResult spawn = null;

                                      try
                                      {
                                          
                                          spawn = isolationCollections[blockCounter].SpawnResultWithBlockBString(currentLine);
                                      }
                                      catch (Exception e)
                                      {
                                          if (e.Message.StartsWith("Cannot spawn ")) //this can happen if we get a messed up record, just make it a warning
                                          {

                                              if(!haveComplainedAboutSpawning)
                                              {
                                                  Warning(this, e.Message);
                                                  haveComplainedAboutSpawning = true;
                                              }
                                          }

                                      }
                                      if (spawn != null)
                                          yield return spawn;
                                  }

                                  //Note that this collection has now been used to spawn records (every collection "$CULT                   SAUR|P|Y|Y" should be followed by a block with at least 1 result (usually lots more)
                              }


                              //read next line
                              currentLine = ReadLine();
                              
                              //deal with what is on the new line - decide if we have transitioned ino BlockB and if we are missing data -- expected a block but transitioned into something else
                              if(currentLine.StartsWith("$SENS"))
                                  if (areInBlockA)
                                      areInBlockA = false; //we are now in Block B
                                  else
                                  {
                                      //we did spawn some so we are transitioning into the next BlockB set
                                      blockCounter++;
                                  }
                          }

                                     
                          end_of_tests = true;
                      }
                }

                  if (!end_of_tests) 
                      currentLine = ReadLine();
              }

              //these will go into the Comment field
              string nwc = "";

              while (currentLine != null &&!is_date(currentLine))
              {
                  nwc += currentLine + " ";
                  currentLine = ReadLine();
              }

              if (currentLine == null)
              {
                  Warning(this, "End of file reached halfway through an MB_Lab record population, record will be discarded");
                  yield break;
              }

              lab.RcvDate = DateOrNull(currentLine);

              string[] lines = ReadLines();

              lab.SpecimenType = lines[0];
              lab.RcvTime = lines[1];
              int len = lines.Length;
              if (lines[len - 1] == "") len--;
              lab.Source = lines[len - 5];
              lab.Dept = lines[len - 4];
              lab.SampleDate = DateOrNull(lines[len - 3]);
              lab.SampleTime = lines[len - 2];
              lab.Clinician = lines[len - 1];
              string comment = "";
              if (len - 7 > 0)
                  comment = string.Join(" ", lines, 2, len - 7);
              nwc += comment;
              nwc = nwc.Replace(',', '-');

              lab.Comments = nwc;

              if (!string.IsNullOrWhiteSpace(testComments))
                  lab.Comments += "###" + testComments;

              lab.Comments = lab.Comments.Trim();

              //dont put in hundreds of empty spaces
              if (string.IsNullOrWhiteSpace(lab.Comments))
                  lab.Comments = null;

              //yield result
              yield return lab;
              
              //if there are no more to come in file, stop
              if (specimen == null)
                  break;
          }
         
      }

       
       private string ReadLine()
       {
           LineNumber++;
           return _textReader.ReadLine();
       }

       private DateTime? DateOrNull(string readLine)
       {
           string toReturn = TrimOrNullify(readLine);

           DateTime dateTime;

           if (toReturn == null)
               return null;

           if (DateTime.TryParse(toReturn, out dateTime))
               return dateTime;
           
           return null;
       }

       private string TrimOrNullify(string readLine)
       {
           if (string.IsNullOrWhiteSpace(readLine))
               return null;
           return readLine.Trim();
       }

       
      /// <summary>
      /// This reads lines at the end of the record until the start of the next record is detected
      /// When this happens the specimen number at the start of the next record is saved
      /// If we hit end-of-file, specimen number is null
      /// </summary>
      /// <returns>Array of strings, last line may be blank</returns>
      private string[] ReadLines()
      {
         bool last_line_blank = false;
         List<string> lines = new List<string>();
         specimen = null;
         for (; ; )
         {
            string st = ReadLine();
            if (st == null) break;
            st = st.Trim();


             //if this is a header skip it and the next line
             if (st.Equals("The following record ids do not exist:"))
             {
                 last_line_blank = true; //treat this line as blank and nuke the next line too
                 ReadLine();
                 continue;
             }
             //if it is a statement about the number of records listed by the lab machine who cares
             if (Regex.IsMatch(st, @"^\d+ records listed"))
                 continue;

            if(IsSpecimenNumber_AKAStartsWith2DigitsThenALetterLawl(st))
                if(last_line_blank)
                {
                   specimen = st;
                   break;
                }
            //if last line was blank and this line is blank too then stop adding blanks to the damn list!
            if (last_line_blank && st == "")
                 continue;
             
            //add line to array of lines to return
            lines.Add(st);
            last_line_blank = st == "";
         }
         return lines.ToArray();
      }

      /*
      bool is_date(string st)
      {
         if (st.Length == 0) return false;
         return st.IndexOf('/') != -1 && char.IsDigit(st[0]);
      }
       */

      bool is_date(string st)
      {
          DateTime whoCares;
          return DateTime.TryParseExact(st, "dd/MM/yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out whoCares);
      }



        private const string specimenRegexPattern = @"^\s*\d{2}[A-Za-z]";
       
        bool IsSpecimenNumber_AKAStartsWith2DigitsThenALetterLawl(string st)
        {
            return st != null && Regex.IsMatch(st.Trim(), specimenRegexPattern);
        }
        public string GetSpecimenNo(TextReader tr)
        {
            string currentLine = null;
           
            //while there are more lines to read
            while ((currentLine = tr.ReadLine()) != null)
            {
                //if line starts with optional whitespace followed by 2 digits and then a character
                if (Regex.IsMatch(currentLine,specimenRegexPattern))
                    return currentLine.Trim();
            }

            return null;
        }
   }

    public delegate void WarningHandler(object sender, string message);
}
