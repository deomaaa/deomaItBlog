document.querySelector(".blog__post__input__button input").addEventListener("click", (target) => {
    let post_id = document.querySelector(".blog__post__id").textContent;
    fetch(`/Blog/PostContent/${post_id}`);
});