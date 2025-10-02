//===============================================================================================================
// System  : Sandcastle Help File Builder Presentation Style Mods Plug-In
// File    : CustomElement.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 09/30/2025
// Note    : Copyright 2022-2025, Eric Woodruff, All rights reserved
//
// This file contains a simple example of a MAML and XML comments element handler
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

using System;
using System.Xml.Linq;

using Sandcastle.Core.PresentationStyle.Transformation;
using Sandcastle.Core.PresentationStyle.Transformation.Elements;
using Sandcastle.Core.Project;

namespace PresentationStyleMods;

/// <summary>
/// This is used as a simple example of a custom XML comments and MAML element handler
/// </summary>
public class CustomElement : Element
{
    /// <summary>
    /// Constructor
    /// </summary>
    public CustomElement() : base("customElement")
    {
    }

    /// <inheritdoc />
    public override void Render(TopicTransformationCore transformation, XElement element)
    {
        if(transformation == null)
            throw new ArgumentNullException(nameof(transformation));

        if(element == null)
            throw new ArgumentNullException(nameof(element));

        // Rendering can be adjusted based on the help file format
        switch(transformation.SupportedFormats)
        {
            case HelpFileFormats.OpenXml:
            case HelpFileFormats.Markdown:
                // No custom formatting support for these so just render the element content
                transformation.RenderChildElements(transformation.CurrentElement, element.Nodes());
                break;

            default:
                // Help 1/Website
                var span = new XElement("span",
                    new XAttribute("class", element.Attribute("style")?.Value ?? "Style1"));

                transformation.CurrentElement.Add(span);
                transformation.RenderChildElements(span, element.Nodes());
                break;
        }

    }
}
