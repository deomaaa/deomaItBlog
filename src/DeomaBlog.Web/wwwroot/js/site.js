/* deomaBlog — progressive enhancement only: the site is fully usable without JS. */
(function () {
    'use strict';

    // Add a copy button to every code block inside an article.
    function enhanceCodeBlocks() {
        var blocks = document.querySelectorAll('.post__body pre');

        Array.prototype.forEach.call(blocks, function (block) {
            var button = document.createElement('button');
            button.type = 'button';
            button.className = 'code-copy';
            button.textContent = 'copy';
            button.style.cssText = [
                'position:absolute', 'top:.4em', 'right:.4em', 'font:inherit',
                'font-size:.72em', 'color:#9a9a9a', 'background:#222', 'cursor:pointer',
                'border:1px solid #3a3a3a', 'border-radius:3px', 'padding:.15em .5em'
            ].join(';');

            button.addEventListener('click', function () {
                var code = block.querySelector('code');
                var text = code ? code.innerText : block.innerText;

                if (!navigator.clipboard) {
                    return;
                }

                navigator.clipboard.writeText(text).then(function () {
                    button.textContent = 'copied';
                    window.setTimeout(function () { button.textContent = 'copy'; }, 1500);
                });
            });

            block.appendChild(button);
        });
    }

    // "/" focuses the search box on the posts page.
    function searchShortcut() {
        var input = document.querySelector('.search__input');

        if (!input) {
            return;
        }

        document.addEventListener('keydown', function (event) {
            var typing = /^(INPUT|TEXTAREA|SELECT)$/.test(document.activeElement.tagName);

            if (event.key === '/' && !typing) {
                event.preventDefault();
                input.focus();
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        enhanceCodeBlocks();
        searchShortcut();
    });
})();
