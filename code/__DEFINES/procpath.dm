/// Represents a proc or verb path.
///
/// Despite having no DM-defined static type, proc paths have some variables,
/// listed below. These are not modifiable, but for a given procpath P,
/// `new P(null, "Name", "Desc")` can be used to create a new procpath with the
/// same code but new `name` and `desc` values. The other variables cannot be
/// changed in this way.
///
/// This type exists only to act as an annotation, providing reasonable static
/// typing for procpaths. Previously, types like `/atom/verb` were used, with
/// the `name` and `desc` vars of `/atom` thus being accessible. Proc and verb
/// paths will fail `istype` and `ispath` checks against `/procpath`.
/procpath
	// Although these variables are effectively const, if they are marked const
	// below, their accesses are optimized away.

	/// A text string of the verb's name.
	var/name as text
	/// The verb's help text or description.
	var/desc as text
	/// The category or tab the verb will appear in.
	var/category as text
	/// Only clients/mobs with `see_invisibility` higher can use the verb.
	var/invisibility as num
