using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using AcTools.Utils.Helpers;
using CefSharp;
using CefSharp.Wpf;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace AcManager.Controls.UserControls.CefSharp {
    public class AcApiHandlerFactory : ISchemeHandlerFactory {
        public const string AcSchemeName = "ac";

        private class Registered {
            public readonly WeakReference<ChromiumWebBrowser> Browser;

            [CanBeNull]
            public readonly string[] AllowedHosts;

            public readonly Func<string, string> Callback;

            public Registered(WeakReference<ChromiumWebBrowser> browser, string[] allowedHosts, Func<string, string> callback) {
                Browser = browser;
                AllowedHosts = allowedHosts;
                Callback = callback;
            }
        }

        private readonly List<Registered> _listeners = new List<Registered>(5);

        public void Register([NotNull] ChromiumWebBrowser browser, [CanBeNull] string[] allowedHosts, Func<string, string> callback) {
            if (browser == null) throw new ArgumentNullException(nameof(browser));
            _listeners.RemoveAll(x => !x.Browser.TryGetTarget(out var b) || ReferenceEquals(b, browser));
            if (allowedHosts?.Length > 0) {
                var hosts = allowedHosts.ArrayContains(@"*") ? null : allowedHosts.Select(x => x.GetDomainNameFromUrl()).Distinct().ToArray();
                _listeners.Add(new Registered(new WeakReference<ChromiumWebBrowser>(browser), hosts, callback));
            }
        }

        IResourceHandler ISchemeHandlerFactory.Create(IBrowser browser, IFrame frame, string schemeName, IRequest request) {
            if (schemeName == AcSchemeName) {
                for (var i = 0; i < _listeners.Count; i++) {
                    var listener = _listeners[i];
                    if (!listener.Browser.TryGetTarget(out var targetBrowser)) {
                        _listeners.RemoveAt(i);
                        continue;
                    }

                    if (targetBrowser.GetBrowser().Identifier == browser.Identifier) {
                        var hostName = frame.Url.GetDomainNameFromUrl();
                        if (listener.AllowedHosts?.Contains(hostName, StringComparer.OrdinalIgnoreCase) != false) {
                            try {
                                var e = listener.Callback(request.Url);
                                if (e != null) {
                                    return new AcResourceHandler(e);
                                }
                            } catch (Exception e) {
                                Logging.Error(e);
                            }
                        }
                    }
                }
            }

            return null;
        }

        private class AcResourceHandler : IResourceHandler {
            private readonly byte[] _response;

            public AcResourceHandler(string response) {
                _response = response == null ? null : Encoding.UTF8.GetBytes(response);
            }

            bool IResourceHandler.ProcessRequest(IRequest request, ICallback callback) {
                callback.Continue();
                return true;
            }

            void IResourceHandler.GetResponseHeaders(IResponse response, out long responseLength, out string redirectUrl) {
                redirectUrl = null;
                if (_response == null || _response.Length == 0) {
                    response.StatusCode = 204;
                    response.StatusText = @"No Content";
                    response.ResponseHeaders = new NameValueCollection();
                    responseLength = 0L;
                } else {
                    response.MimeType = @"text/html; charset=utf-8";
                    response.StatusCode = 200;
                    response.StatusText = @"OK";
                    response.ResponseHeaders = new NameValueCollection();
                    responseLength = _response.Length;
                }
            }

            bool IResourceHandler.ReadResponse(Stream dataOut, out int bytesRead, ICallback callback) {
                if (!callback.IsDisposed) {
                    callback.Dispose();
                }

                if (_response == null) {
                    bytesRead = 0;
                    return false;
                }

                using (var s = new MemoryStream(_response)) {
                    bytesRead = s.CopyTo(dataOut, (int)dataOut.Length, 8192);
                    return bytesRead > 0;
                }
            }

            bool IResourceHandler.CanGetCookie(Cookie cookie) => true;
            bool IResourceHandler.CanSetCookie(Cookie cookie) => true;
            void IResourceHandler.Cancel() {}
            void IDisposable.Dispose() {}
        }
    }
}