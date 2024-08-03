/**
 * Depends on wave.js
 * Script specifically for comments page
 */
    
ready(function () {
    /** @type {HTMLFormElement} */
    const commentForm = document.getElementById("comment-form")
    const commentList = document.getElementById("comment-list")
    commentForm.addEventListener("submit", async function (event) {
        event.preventDefault()

        const res = await fetch(commentForm.action, {
            method: "POST",
            body: new FormData(commentForm),
            headers: { WaveJavascript: "" },
        })

        const redirect = res.headers.get("WaveRedirect")
        if (redirect) {
            window.location.href = redirect
            return
        }

        const html = await res.text()
        if (res.redirected) {
            replaceDocumentHtml(html)
            pushCurrentDocumentToHistory(res.url)
        } else if (res.ok) {
            commentList.insertAdjacentHTML("afterbegin", html)
        } else {
            replaceDocumentHtml(html)
            pushCurrentDocumentToHistory(res.url)
        }

        commentForm.reset()
    })
})
