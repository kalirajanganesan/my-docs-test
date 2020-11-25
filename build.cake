//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
#addin nuget:?package=Cake.FileHelpers
#tool nuget:?package=Syncfusion.Spellcheck.CI
var target = Argument("target", "Default");
var reposistoryPath=MakeAbsolute(Directory("../"));
#tool nuget:?package=Syncfusion.Content.DocumentValidation.CI
#tool nuget:?package=Syncfusion.Content.FTHtmlConversion.CI
#tool nuget:?package=Syncfusion.PushGitLabToGithub
var cireports = Argument("cireports", "../cireports");
var platform=Argument<string>("platform","");
var sourcebranch=Argument<string>("branch","");
var targetBranch=Argument<string>("targetbranch","");
var buildStatus = true;
var isSpellingError=0;
var isDocumentvalidationError=0;
var isHtmlConversionError=0;
var isGithubMoveStatus=0;
var sourcefolder="";
var repositoryName="";

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////
using System.IO;
//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////


Task("build")
    .Does(() =>
{   
 CopyFiles("./tools/syncfusion.spellcheck.ci/Syncfusion.Spellcheck.CI/content/*", "./tools");
 CopyFiles("./tools/syncfusion.spellcheck.ci/Syncfusion.Spellcheck.CI/lib/*", "./tools");
 CopyFiles("./tools/Syncfusion.Content.DocumentValidation.CI/Syncfusion.Content.DocumentValidation.CI/content/*", "./");
 CopyFiles("./tools/Syncfusion.Content.DocumentValidation.CI/Syncfusion.Content.DocumentValidation.CI/lib/*", "./");
 CopyFiles("./tools/Syncfusion.Content.FTHtmlConversion.CI/Syncfusion.Content.FTHtmlConversion.CI/content/*", "./");
 CopyFiles("./tools/Syncfusion.Content.FTHtmlConversion.CI/Syncfusion.Content.FTHtmlConversion.CI/lib/*", "./");
 CopyFiles("./tools/Syncfusion.PushGitLabToGithub/Syncfusion.PushGitLabToGithub/tools/*", "./tools");
 EnsureDirectoryExists("./Templates");
 CopyFiles("./tools/Syncfusion.Content.DocumentValidation.CI/Syncfusion.Content.DocumentValidation.CI/Templates/*", "./Templates");
 EnsureDirectoryExists("./HtmlConvertionTemplates");
 CopyFiles("./tools/Syncfusion.Content.FTHtmlConversion.CI/Syncfusion.Content.FTHtmlConversion.CI/HtmlConvertionTemplates/*", "./HtmlConvertionTemplates");
  
  
  var directories = GetSubDirectories(reposistoryPath);
  foreach(var repository in directories)
    {
	 if(!repository.ToString().Contains("ug_spellchecker")&&!repository.ToString().Contains("cireports"))
	 {
	  sourcefolder=repository.ToString();
	 }
    }
    try
    {
        //Code to run spellchecker tool
        isSpellingError=StartProcess("./tools/DocumentSpellChecker.exe",new ProcessSettings{ Arguments = "/IsCIOperation:true /platform:"+platform+" /branch:"+sourcebranch+" /sourcefolder:"+sourcefolder});
        
        //Code to run the Document validation tool
        repositoryName =reposistoryPath.ToString().Split('/')[3].Split('@')[0];
        isDocumentvalidationError=StartProcess("./DocumentationValidation.exe",new ProcessSettings{ Arguments = reposistoryPath+"/Spell-Checker/ "+repositoryName+" "+targetBranch});
		
	bool isWithoutError = true;

        var errorfiles = GetFiles("../cireports/errorlogs/*.txt");
		
	if(!(errorfiles.Count() > 0))
        {
            var reportFiles = GetFiles(@"../cireports/**/*.(htm||html)");
				
            foreach (var reportFile in reportFiles)
            {
                string fileContent = System.IO.File.ReadAllText(reportFile.ToString());
										
                if ((fileContent.Contains("</td>")))
                {
                    if ((!reportFile.ToString().Contains("spellcheckreport")) || (fileContent.Contains("<td>Technical Error</td>") || fileContent.Contains("<td>Spell Error</td>")))
                    {
                        isWithoutError = false;
                        break;
                    }
                }
            }
            if (isWithoutError == true)
            {
		//Code to run the Html conversion tool for feature tour repositories
		if (((repositoryName.ToLower().Contains("featuretour")) && targetBranch.ToLower() == "development"))
		{
			isHtmlConversionError=StartProcess("./MDToHtmlConverter.exe",new ProcessSettings{ Arguments = reposistoryPath+"/Spell-Checker/ "+repositoryName+" "+reposistoryPath+"/FTautomation/Automation"});
		}
            }
	  }
	}
	catch(Exception ex)
	{        
		buildStatus = false;
		Information(ex);
	}
	if(isSpellingError==0 && isDocumentvalidationError==0 && isHtmlConversionError==0 && buildStatus) {    
		Information("Compilation successfull");
		RunTarget("CopyFile");
		if(targetBranch.Contains("master")&&sourcebranch.Contains("master"))
		{
		  RunTarget("MoveGitlabToGithub");
		}
	} 
	else {   
		throw new Exception(String.Format("Please fix the project compilation failures"));  
	}
});

Task("CopyFile")
.Does(() =>
{
		if (!DirectoryExists(cireports))
		{			
			CreateDirectory(cireports);
		}

		EnsureDirectoryExists(cireports+"/spellcheck/");
		
		if (FileExists(cireports+"/spellcheckreport.htm"))
		{
			MoveFileToDirectory(cireports+"/spellcheckreport.htm", cireports+"/spellcheck/");
		}
		
		
});

Task("MoveGitlabToGithub")
.Does(() =>
{
	try {
            
			    repositoryName =reposistoryPath.ToString().Split('/')[3].Split('@')[0];
                Information("Moving Files from Gitlab to Github");
				Information("Cloning repository.."+repositoryName);
				Information("Cloning repository.."+reposistoryPath);
			    isGithubMoveStatus=StartProcess("./tools/PushGitLabToGithub.exe",new ProcessSettings{ Arguments = reposistoryPath+"/Spell-Checker/"+" "+repositoryName});
            
		}
	catch(Exception ex)
	{        
		buildStatus = false;
	}	
		
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
