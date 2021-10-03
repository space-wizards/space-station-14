using System;
using System.Collections;
using System.Collections.Generic;

namespace Content.Server.PDA
{
    public sealed class PDAAccessSet : ISet<string>
    {
        private readonly PDAComponent _pdaComponent;
        private static readonly HashSet<string> EmptySet = new();

        public PDAAccessSet(PDAComponent pdaComponent)
        {
            _pdaComponent = pdaComponent;
        }

        public IEnumerator<string> GetEnumerator()
        {
            var contained = _pdaComponent.GetContainedAccess() ?? EmptySet;
            return contained.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<string>.Add(string item)
        {
            throw new NotSupportedException("PDA access list is read-only.");
        }

        public void ExceptWith(IEnumerable<string> other)
        {
            throw new NotSupportedException("PDA access list is read-only.");
        }

        public void IntersectWith(IEnumerable<string> other)
        {
            throw new NotSupportedException("PDA access list is read-only.");
        }

        public bool IsProperSubsetOf(IEnumerable<string> other)
        {
            var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
            return set.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<string> other)
        {
            var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
            return set.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<string> other)
        {
            var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
            return set.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<string> other)
        {
            var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
            return set.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<string> other)
        {
            var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
            return set.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<string> other)
        {
            var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
            return set.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<string> other)
        {
            throw new NotSupportedException("PDA access list is read-only.");
        }

        public void UnionWith(IEnumerable<string> other)
        {
            throw new NotSupportedException("PDA access list is read-only.");
        }

        bool ISet<string>.Add(string item)
        {
            throw new NotSupportedException("PDA access list is read-only.");
        }

        public void Clear()
        {
            throw new NotSupportedException("PDA access list is read-only.");
        }

        public bool Contains(string item)
        {
            return _pdaComponent.GetContainedAccess()?.Contains(item) ?? false;
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
            set.CopyTo(array, arrayIndex);
        }

        public bool Remove(string item)
        {
            throw new NotSupportedException("PDA access list is read-only.");
        }

        public int Count => _pdaComponent.GetContainedAccess()?.Count ?? 0;
        public bool IsReadOnly => true;
    }
}
