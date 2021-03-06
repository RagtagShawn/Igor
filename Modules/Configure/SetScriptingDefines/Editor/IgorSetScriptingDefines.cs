using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System;
using System.Reflection;
using System.Xml.Serialization;

namespace Igor
{
	public class IgorSetScriptingDefines : IgorModuleBase
	{
        public static StepID SetScriptingDefinesStep = new StepID("SetScriptingDefines", 270);

        static List<BuildTargetGroup> _buildTargets = null;
        public static List<BuildTargetGroup> BuildTargets
        {
            get
            {
                if(_buildTargets == null)
                {
                    _buildTargets = new List<BuildTargetGroup>();

                    var values = System.Enum.GetValues(typeof(BuildTargetGroup));
                    foreach(BuildTargetGroup value in values)
                    {
                        _buildTargets.Add(value);
                    }
                }
                return _buildTargets;
            }
        }

		static string SetScriptingDefinesFlag = "scripting_defines";

		public override string GetModuleName()
		{
			return "Configure.SetScriptingDefines";
		}

		public override void RegisterModule()
		{
			IgorCore.RegisterNewModule(this);
		}

		public override void ProcessArgs(IIgorStepHandler StepHandler)
		{
			if(IgorJobConfig.IsStringParamSet(SetScriptingDefinesFlag) || StepHandler.IsModuleNeededByOtherModules(this))
			{
				IgorCore.SetModuleActiveForJob(this);
				StepHandler.RegisterJobStep(SetScriptingDefinesStep, this, SetScriptingDefines);
			}
		}

        public override void PostJobCleanup()
        {
            ExtraModuleParams = "";

            // Record pre-job-run scripting define symbols per build target so that if someone wants to test a job
            // in editor it doesn't leave the job's defines resident in the PlayerSettings afterwards.
            foreach(BuildTargetGroup group in BuildTargets)
            {
                if(group != BuildTargetGroup.Unknown)
                {
                    string Key = kEditorPrefsPrefix  + group.ToString();
                    if(EditorPrefs.HasKey(Key))
                    {
                        string CachedDefines = EditorPrefs.GetString(Key, string.Empty);
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, CachedDefines);
                    }
                }
            }   
        }

		public override string DrawJobInspectorAndGetEnabledParams(string CurrentParams)
		{
			string EnabledParams = CurrentParams;
            DrawStringParam(ref EnabledParams, "Defines", SetScriptingDefinesFlag);
			return EnabledParams;
		}

        static string kEditorPrefsPrefix = "PreJobScriptingDefines_";

        public static string ExtraModuleParams = "";

	    public virtual bool SetScriptingDefines()
	    {
            // Record pre-job-run scripting define symbols per build target so that if someone wants to test a job
            // in editor it doesn't leave the job's defines resident in the PlayerSettings afterwards.
            foreach(BuildTargetGroup group in BuildTargets)
            {
                if(group != BuildTargetGroup.Unknown)
                    EditorPrefs.SetString(kEditorPrefsPrefix  + group.ToString(), PlayerSettings.GetScriptingDefineSymbolsForGroup(group));
            }

            string AllParams = ExtraModuleParams;

            if(AllParams.Length > 0)
            {
                AllParams += ";";
            }

            AllParams += IgorJobConfig.GetStringParam(SetScriptingDefinesFlag);

            // Flush to all of the groups, to be sure we hit the one marked for build in the job. This should be fine
            // because there's only ever at most one built platform per job.
            foreach(BuildTargetGroup group in BuildTargets)
            {
                if(group != BuildTargetGroup.Unknown)
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, AllParams);
            }
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
	        
            return true;
	    }
	}
}