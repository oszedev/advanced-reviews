﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer;
using EPiServer.Cms.Shell;
using EPiServer.Core;
using EPiServer.Core.Internal;
using EPiServer.DataAbstraction;
using EPiServer.Filters;
using EPiServer.Globalization;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;

namespace AdvancedExternalReviews.DraftContentAreaPreview
{
    [ServiceConfiguration(typeof(ReviewsContentLoader))]
    public class ReviewsContentLoader
    {
        private readonly IContentLoader _contentLoader;
        private readonly IContentLanguageAccessor _languageAccessor;
        private readonly ProjectContentResolver _projectContentResolver;
        private readonly IContentVersionRepository _contentVersionRepository;
        private readonly LanguageResolver _languageResolver;
        private readonly IContentProviderManager _contentProviderManager;
        private readonly IContentChildrenSorter _childrenSorter;
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewsContentLoader));

        public ReviewsContentLoader(IContentLoader contentLoader, IContentLanguageAccessor languageAccessor,
            ProjectContentResolver projectContentResolver,
            IContentVersionRepository contentVersionRepository,
            LanguageResolver languageResolver,
            IContentProviderManager contentProviderManager,
            IContentChildrenSorter childrenSorter)
        {
            _contentLoader = contentLoader;
            _languageAccessor = languageAccessor;
            _projectContentResolver = projectContentResolver;
            _contentVersionRepository = contentVersionRepository;
            _languageResolver = languageResolver;
            _contentProviderManager = contentProviderManager;
            _childrenSorter = childrenSorter;
        }

        public IEnumerable<T> GetChildrenWithReviews<T>(ContentReference contentLink) where T : IContentData
        {
            return GetChildrenWithReviews<T>(contentLink, CreateDefaultListOption());
        }

        public IEnumerable<T> GetChildrenWithReviews<T>(ContentReference contentLink, CultureInfo language) where T : IContentData
        {
            return GetChildrenWithReviews<T>(contentLink, new LoaderOptions() {LanguageLoaderOption.Specific(language)}, -1, -1);
        }

        public IEnumerable<T> GetChildrenWithReviews<T>(ContentReference contentLink, CultureInfo language, int startIndex, int maxRows) where T : IContentData
        {
            return GetChildrenWithReviews<T>(contentLink, language);
        }

        public IEnumerable<T> GetChildrenWithReviews<T>(ContentReference contentLink, LoaderOptions loaderOptions) where T : IContentData
        {
            return GetChildrenWithReviews<T>(contentLink, loaderOptions, -1, -1);
        }

        public IEnumerable<T> GetChildrenWithReviews<T>(
            ContentReference contentLink, LoaderOptions loaderOptions, int startIndex, int maxRows) where T : IContentData
        {
            if (ContentReference.IsNullOrEmpty(contentLink))
            {
                throw new ArgumentNullException(nameof(contentLink), "Parameter has no value set");
            }

            if (!ExternalReview.IsInExternalReviewContext)
            {
                return _contentLoader.GetChildren<T>(contentLink);
            }

            ContentReference referenceWithoutVersion = contentLink.ToReferenceWithoutVersion();
            if (referenceWithoutVersion == ContentReference.WasteBasket)
            {
                return _contentLoader.GetChildren<T>(contentLink, loaderOptions, startIndex, maxRows);
            }

            var provider = _contentProviderManager.ProviderMap.GetProvider(referenceWithoutVersion);

            var parentContent = _contentLoader.Get<IContent>(referenceWithoutVersion, loaderOptions);
            var localizable = parentContent as ILocalizable;
            var languageID = localizable != null ? localizable.Language.Name : (string)null;
            var childrenReferences =
                provider.GetChildrenReferences<T>(referenceWithoutVersion, languageID, startIndex, maxRows);

            var result = new List<ContentReference>();
            foreach (var childReference in childrenReferences)
            {
                var referenceToLoad = LoadUnpublishedVersion(childReference.ContentLink);
                if (referenceToLoad == null)
                {
                    result.Add(childReference.ContentLink);
                }
                else
                {
                    var content = _contentLoader.Get<T>(referenceToLoad, loaderOptions);
                    if (content == null)
                    {
                        continue;
                    }

                    if (!(content is IVersionable versionable))
                    {
                        result.Add(childReference.ContentLink);
                        continue;
                    }

                    if (HasExpired(versionable))
                    {
                        continue;
                    }

                    if ((content as IContent).IsPublished())
                    {
                        // for published version return the original method result
                        result.Add(childReference.ContentLink);
                        continue;
                    }

                    result.Add((content as IContent).ContentLink);
                }
            }

            var childrenWithReviews = result.Select(x => _contentLoader.Get<T>(x, loaderOptions)).Where(y => y != null).ToList();


            if (childrenWithReviews.Count > 0)
            {
                var pageData = parentContent as PageData;
                if (pageData != null && pageData.ChildSortOrder == FilterSortOrder.Alphabetical && (startIndex == -1 && maxRows == -1 ||
                                                                                                    childrenWithReviews.Count < maxRows && startIndex == 0))
                {
                    var collection = _childrenSorter.Sort((IEnumerable<IContent>) childrenWithReviews);
                    childrenWithReviews = collection.Cast<T>().ToList();
                }
            }

            return childrenWithReviews;
        }

        public ContentReference LoadUnpublishedVersion(ContentReference baseReference)
        {
            if (ExternalReview.ProjectId.HasValue)
            {
                // load version from project
                return _projectContentResolver.GetProjectReference(baseReference,
                    ExternalReview.ProjectId.Value, _languageResolver.GetPreferredCulture().Name);
            }

            // load common draft instead of published version
            ContentVersion loadCommonDraft;
            try
            {
                loadCommonDraft = _contentVersionRepository.LoadCommonDraft(baseReference, _languageResolver.GetPreferredCulture().Name);
            }
            catch (ContentNotFoundException)
            {
                _log.Debug($"Advanced Reviews: Content {baseReference} not found for LoadUnpublishedVersion");
                loadCommonDraft = null;
            }
            
            if (loadCommonDraft == null)
            {
                // fallback to default implementation if there is no common draft in a given language
                return null;
            }

            return loadCommonDraft.ContentLink;
        }

        public bool HasExpired(IVersionable content)
        {
            return content.Status == VersionStatus.Published && content.StopPublish < DateTime.Now;
        }

        private LoaderOptions CreateDefaultListOption()
        {
            LoaderOptions loaderOptions = new LoaderOptions();
            loaderOptions.Add<LanguageLoaderOption>(LanguageLoaderOption.Fallback(_languageAccessor.Language));
            return loaderOptions;
        }
    }
}
