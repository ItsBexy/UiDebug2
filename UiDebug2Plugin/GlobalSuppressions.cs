// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "i dont care")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial Elements should be documented", Justification = "i dont care")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File should have header", Justification = "i dont care")]
[assembly: SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "DOESN'T WORK")]
[assembly: SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "DOESN'T WORK")]
[assembly: SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "DOESN'T WORK")]
[assembly: SuppressMessage("Style", "IDE0300:Simplify collection initialization", Justification = "DOESN'T WORK")]
[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "it's moving anyway")]
[assembly: SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "broke")]
[assembly: SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "ugh", Scope = "member", Target = "~M:UiDebug2.Browsing.AddonTree.Dispose")]
[assembly: SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "ugh", Scope = "member", Target = "~M:UiDebug2.Browsing.ResNodeTree.Dispose")]
