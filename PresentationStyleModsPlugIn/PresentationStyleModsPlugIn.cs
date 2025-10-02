//===============================================================================================================
// System  : Sandcastle Help File Builder Presentation Style Mods Plug-In
// File    : PresentationStyleModsPlugIn.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/30/2025
// Note    : Copyright 2022-2025, Eric Woodruff, All rights reserved
//
// This file contains the class used to demonstrate how a presentation style can be extended in various ways
// without having to clone and implement an entirely new presentation style.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website:
// https://GitHub.com/EWSoftware/PresentationStyleModsPlugIn.  This notice, the author's name, and all copyright
// notices must remain intact in all applications, documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 08/06/2022  EFW  Created the code
//===============================================================================================================

// Ignore Spelling: stylesheet href html

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Sandcastle.Core;
using Sandcastle.Core.BuildEngine;
using Sandcastle.Core.PlugIn;
using Sandcastle.Core.PresentationStyle.Transformation;
using Sandcastle.Core.PresentationStyle.Transformation.Elements;
using Sandcastle.Core.Project;

namespace PresentationStyleMods;

/// <summary>
/// This plug-in is used to demonstrate how a presentation style can be extended in various ways without
/// having to clone and implement an entirely new presentation style.
/// </summary>
/// <remarks>These are just some test remarks.  This section will be moved by the plug-in.</remarks>
/// <customSection>
/// <para>This is an example of a custom XML comments section.  Our plug-in will render this section
/// including any <customElement style="Style1">custom elements</customElement> that are present.</para>
/// 
/// <para>The custom section can itself contain other standard XML comment elements.</para>
/// 
/// <list type="bullet">
///     <item>An example of the custom element <customElement style="Style1">style 1</customElement>.</item>
///     <item>An example of the custom element <customElement style="Style2">style 2</customElement>.</item>
///     <item>An example of the custom element <customElement style="Style3">style 3</customElement>.</item>
/// </list>
/// </customSection>
[HelpFileBuilderPlugInExport("Presentation Style Mods", Version = AssemblyInfo.ProductVersion,
  Copyright = AssemblyInfo.Copyright, Description = "This plug-in demonstrates various ways that a " +
  "presentation style can be modified and extended without having to clone and implement an entirely new " +
  "presentation style.")]
public sealed class PresentationStyleModsPlugIn : IPlugIn
{
    #region Private data members
    //=====================================================================

    private IBuildProcess builder;

    #endregion

    #region IPlugIn implementation
    //=====================================================================

    /// <summary>
    /// This read-only property returns a collection of execution points that define when the plug-in should
    /// be invoked during the build process.
    /// </summary>
    public IEnumerable<ExecutionPoint> ExecutionPoints { get; } =
    [
        // For presentation style modifications, this is a good step in which to make your
        // changes as it will execute prior to the BuildAssembler configuration file being
        // created.  This gives the plug-in a chance to add resource items files to the
        // presentation style if necessary.
        new ExecutionPoint(BuildStep.CreateBuildAssemblerConfigs, ExecutionBehaviors.Before),
        // Other execution points can be added if needed for other tasks.  This one copies
        // a custom style sheet to the help format output folders.
        new ExecutionPoint(BuildStep.CopyStandardHelpContent, ExecutionBehaviors.After)
    ];

    /// <summary>
    /// This method is used to initialize the plug-in at the start of the build process
    /// </summary>
    /// <param name="buildProcess">A reference to the current build process</param>
    /// <param name="configuration">The configuration data that the plug-in should use to initialize itself</param>
    public void Initialize(IBuildProcess buildProcess, XElement configuration)
    {
        builder = buildProcess;

        var metadata = (HelpFileBuilderPlugInExportAttribute)this.GetType().GetCustomAttributes(
            typeof(HelpFileBuilderPlugInExportAttribute), false).First();

        builder.ReportProgress("{0} Version {1}\r\n{2}", metadata.Id, metadata.Version, metadata.Copyright);
    }

    /// <summary>
    /// This method is used to execute the plug-in during the build process
    /// </summary>
    /// <param name="context">The current execution context</param>
    public void Execute(ExecutionContext context)
    {
        if(context == null)
            throw new ArgumentNullException(nameof(context));

        if(context.BuildStep == BuildStep.CreateBuildAssemblerConfigs)
        {
            var transformation = builder.PresentationStyle.TopicTransformation;

            #region Reorder/remove API topic sections
            //=====================================================================

            // Here is an example of reordering the API topic sections.  Note that MAML topic section ordering is
            // controlled entirely by the structure of the MAML in the topic file so this only applies to API
            // topics.  The default order of the API topic sections is the order of the ApiTopicSectionType
            // enumeration.  Be aware that not all presentation styles may implement all of the section types.

            // Move the remarks section so that it appears just above the See Also section.  Get a reference
            // to the existing handler before removing it.
            ApiTopicSectionHandler remarksHandler = transformation.ApiTopicSectionHandlerFor(ApiTopicSectionType.Remarks, null),
                seeAlsoHandler = transformation.ApiTopicSectionHandlerFor(ApiTopicSectionType.SeeAlso, null);

            // Add it back in the new location.  There is also an InsertApiTopicSectionHandlerAfter method to
            // insert the handler after the given handler.  If the handler is already in the set, it is
            // removed from it's old location before placing it in the new location.
            transformation.InsertApiTopicSectionHandlerBefore(seeAlsoHandler, remarksHandler);

            // Remove a section if you no longer want it included.
            // transformation.RemoveApiTopicSectionHandler(ApiTopicSectionType.Remarks, null);

            #endregion

            #region Add an additional style sheet and resource items file
            //=====================================================================

            // Be sure to include any additional files as content items in your project so that they are
            // deployed with the plug-in.

            // The style sheet will be inserted when the topic is rendered
            transformation.RenderStarting += this.Transformation_RenderStarting;

            // Resource items are added here and will be used when the include items are resolved.  Use
            // the resource items file based on the language or fall back to the English resources if a
            // localized version does not exist.
            string resourceItems = Path.Combine(ComponentUtilities.AssemblyFolder(null), "Resources",
                $"PresentationStyleMods-{builder.Language.Name}.xml");

            if(!File.Exists(resourceItems))
            {
                resourceItems = Path.Combine(ComponentUtilities.AssemblyFolder(null), "Resources",
                    "PresentationStyleMods-en-US.xml");
            }

            builder.PresentationStyle.AdditionalResourceItemsFiles.Add(resourceItems);

            #endregion

            #region Add a custom XML comments section handler
            //=====================================================================

            // Add a custom section after the summary section
            var summaryHandler = transformation.ApiTopicSectionHandlerFor(ApiTopicSectionType.Summary, null);

            // Use the CustomSection section type and give it a unique name for use in locating it
            transformation.InsertApiTopicSectionHandlerAfter(summaryHandler,
                new ApiTopicSectionHandler(ApiTopicSectionType.CustomSection, "PresentationStyleModsCustomSection",
                    RenderCustomSection));
            #endregion

            #region Add custom element handlers
            //=====================================================================

            // Add a custom named section for MAML topics.  This is equivalent to the XML comments section
            // handler.  MAML topics render everything through element handlers including sections like
            // this one.  We'll use the standard named section handler element.
            transformation.AddElement(new NamedSectionElement("customSection"));

            // Add an inline element handler.  This works for MAML or XML comments.
            transformation.AddElement(new CustomElement());

            #endregion
        }
        else
        {
            // Only copy the style sheet for Help 1 and Website output
            if((builder.CurrentProject.HelpFileFormat & (HelpFileFormats.HtmlHelp1 | HelpFileFormats.Website)) != 0)
            {
                // Copy the custom style sheet to the working folder
                string stylesheet = Path.Combine(ComponentUtilities.AssemblyFolder(null), "Resources",
                    "PresentationStyleMods.css");

                foreach(string path in builder.HelpFormatOutputFolders)
                {
                    string destPath = Path.Combine(path, "html",
                        builder.PresentationStyle.TopicTransformation.StyleSheetPath,
                        Path.GetFileName(stylesheet)).Replace("/", "\\");

                    builder.ReportProgress(path);

                    File.Copy(stylesheet, destPath);
                }
            }
        }
    }
    #endregion

    #region IDisposable implementation
    //=====================================================================

    /// <summary>
    /// This handles garbage collection to ensure proper disposal of the plug-in if not done explicitly
    /// with <see cref="Dispose()"/>.
    /// </summary>
    ~PresentationStyleModsPlugIn()
    {
        // If the plug-in hasn't got any disposable resources, this finalizer can be removed
        this.Dispose();
    }

    /// <summary>
    /// This implements the Dispose() interface to properly dispose of the plug-in object
    /// </summary>
    public void Dispose()
    {
        // Dispose of any resources here if necessary
        GC.SuppressFinalize(this);
    }
    #endregion

    #region Custom presentation style handlers
    //=====================================================================

    /// <summary>
    /// This event handler is used to add a new style sheet reference to the rendered topic
    /// </summary>
    /// <param name="sender">The sender of the event</param>
    /// <param name="e">The event arguments</param>
    private void Transformation_RenderStarting(object sender, RenderTopicEventArgs e)
    {
        var transformation = builder.PresentationStyle.TopicTransformation;

        // Only add it for Help 1 and Website output
        if((transformation.SupportedFormats & (HelpFileFormats.HtmlHelp1 | HelpFileFormats.Website)) != 0)
        {
            var head = e.TopicContent.Root.Element("head");

            head?.Add(new XElement("link",
                new XAttribute("rel", "stylesheet"),
                new XAttribute("href", $"{transformation.StyleSheetPath}PresentationStyleMods.css")));
        }
    }

    /// <summary>
    /// This is used to handle rendering of a custom XML comments section
    /// </summary>
    /// <param name="transformation">The current transformation used to render the section</param>
    private void RenderCustomSection(TopicTransformationCore transformation)
    {
        var customSection = transformation.CommentsNode.Element("customSection");

        if(customSection != null)
        {
            // Create a section with an optional title
            var (title, content) = transformation.CreateSection("CS_" + customSection.GenerateUniqueId(), true,
                "title_customSection", null);

            if(title != null)
                transformation.CurrentElement.Add(title);

            if(content != null)
                transformation.CurrentElement.Add(content);

            // Render the child elements of the custom section into the content element.  Any additional
            // rendering tasks could be performed here as well.  If the section contains custom elements,
            // use element handlers to render them.
            transformation.RenderChildElements(content ?? transformation.CurrentElement, customSection.Nodes());
        }
    }
    #endregion
}
