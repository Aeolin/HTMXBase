#!/bin/sh
user_name="admin"
user_password="@admin13"
user_email="admin@localhost"
api_url="http://htmxbase:8080"
#api_url="https://$(hostname).local:7110" # uncomment when using script from wsl

echo "Sleeping for 10 seconds to ensure backend is started..."
sleep 10 # hacky way to wait for backend to be started

curl -k  --request POST \
  --url $api_url/api/v1/auth/register \
  --form "Email=\"$user_email\"" \
  --form "Password=\"$user_password\"" \
  --form "Username=$user_name" \
  --form "FirstName=Admin" \
  --form "LastName=System" 

jwt=$(curl -k --request POST \
  --url $api_url/api/v1/auth/login?useCookie=false \
  --form "UsernameOrEmail=\"$user_email\"" \
  --form "Password=\"$user_password\""
)

curl -k --request PUT \
  --url $api_url/api/v1/admin/groups/user/permissions \
  --header "Content-Type: application/json" \
  --header "Authorization: Bearer $jwt" \
  --data '{"Permissions":["blog/user", "blog/comment"]}'

curl -k --request PUT \
  --url $api_url/api/v1/admin/groups/admin/permissions \
  --header "Content-Type: application/json" \
  --header "Authorization: Bearer $jwt" \
  --data '{"Permissions": ["blog/moderator", "blog/admin"]}'

jwt=$(curl -k --request POST \
  --url $api_url/api/v1/auth/login?useCookie=false \
  --form "UsernameOrEmail=\"$user_email\"" \
  --form "Password=\"$user_password\""
)


schema=$(cat post_schema.json | jq -c .)
template_detail=$(jq -Rs . < post_detail_template.html)
template_overview=$(jq -Rs . < post_overview_template.html)

json_body=$(cat <<EOF
{
  "Slug": "posts",
  "Name": "Posts",
  "Schema": $schema,
  "Templates": [
    {
      "Slug": "post-detail",
      "SingleItem": true,
      "Template": $template_detail
    },
    {
      "Slug": "posts",
      "SingleItem": true,
      "Template": $template_overview
    }
  ],
  "InsertPermission": "blog/user",
  "ModifyPermission": "blog/moderator",
  "DeletePermission": "blog/moderator",
  "ComplexQueryPermission": "blog/admin"
}
EOF
)

curl -k --request POST \
  --url "$api_url/api/v1/collections" \
  --header "Authorization: Bearer $jwt" \
  --header "Content-Type: application/json" \
  --data "$json_body"

schema=$(cat comment_schema.json | jq -c .)
template_detail=$(jq -Rs . < comments_template.html)
template_overview=$(jq -Rs . < comment_template.html)

json_body=$(cat <<EOF
{
  "Slug": "comments",
  "Name": "Comments",
  "Schema": $schema,
  "Templates": [
    {
      "Slug": "comments",
      "SingleItem": true,
      "Template": $template_detail
    },
    {
      "Slug": "comment",
      "SingleItem": true,
      "Template": $template_overview
    }
  ],
  "DefaultTemplate": "comment",
  "InsertPermission": "blog/user",
  "ModifyPermission": "blog/moderator",
  "DeletePermission": "blog/moderator",
  "ComplexQueryPermission": "blog/admin"
}
EOF
)
curl -k --request POST \
  --url "$api_url/api/v1/collections" \
  --header "Authorization: Bearer $jwt" \
  --header "Content-Type: application/json" \
  --data "$json_body"


curl -k --request POST \
  --url $api_url/api/v1/files \
  --header 'Content-Type: multipart/form-data' \
  --header "Authorization: Bearer $jwt" \
  --form "file=@./blog.html" \
  --form "slug=blog.html" \
  --form "virtualPath=static-content"
  
curl -k --request POST \
  --url $api_url/api/v1/files \
  --header 'Content-Type: multipart/form-data' \
  --header "Authorization: Bearer $jwt" \
  --form "file=@./styles.css" \
  --form "slug=styles.css" \
  --form "virtualPath=static-content"
  
curl -k --request POST \
  --url $api_url/api/v1/files \
  --header 'Content-Type: multipart/form-data' \
  --header "Authorization: Bearer $jwt" \
  --form "file=@./login.html" \
  --form "slug=login.html" \
  --form "virtualPath=static-content"
  
curl -k --request POST \
  --url $api_url/api/v1/files \
  --header 'Content-Type: multipart/form-data' \
  --header "Authorization: Bearer $jwt" \
  --form "file=@./register.html" \
  --form "slug=register.html" \
  --form "virtualPath=static-content"
  
curl -k --request POST \
  --url $api_url/api/v1/files \
  --header 'Content-Type: multipart/form-data' \
  --header "Authorization: Bearer $jwt" \
  --form "file=@./create_post.html" \
  --form "slug=create-post.html" \
  --form "virtualPath=static-content"  
  
json_body=$(cat <<EOF
{
	"UrlTemplate": "blog/posts/{postSlug}",
	"CollectionSlug": "posts",
	"TemplateSlug": "post-detail",
	"BaseTemplatePathTemplate": "static-content/blog.html",
	"Paginate": false,
	"Fields": [
		{
			"ParameterName": "postSlug",
			"DocumentFieldName": "slug",
			"MatchKind": 0,
			"BsonType": 2,
			"IsNullable": false,
			"IsOptional": false,
			"Value": null
		}
	]
}
EOF
)
curl -k --request POST \
  --url "$api_url/api/v1/routes" \
  --header "Authorization: Bearer $jwt" \
  --header "Content-Type: application/json" \
  --data "$json_body"

json_body=$(cat <<EOF
{	
	"UrlTemplate": "index.html",
	"RedirectUrlTemplate": "/blog/posts"	
}
EOF
)
curl -k --request POST \
  --url $api_url/api/v1/routes \
  --header "Authorization: Bearer $jwt" \
  --header "Content-Type: application/json" \
  --data "$json_body"

json_body=$(cat <<EOF
{
	"UrlTemplate": "blog/posts",
	"CollectionSlug": "posts",
	"TemplateSlug": "posts",
	"BaseTemplatePathTemplate": "static-content/blog.html",
	"Paginate": true
}
EOF
)
curl -k --request POST \
  --url $api_url/api/v1/routes \
  --header "Authorization: Bearer $jwt" \
  --header "Content-Type: application/json" \
  --data "$json_body"

json_body=$(cat <<EOF
{
	"UrlTemplate": "blog/posts/{postId}/comments",
	"CollectionSlug": "comments",
	"TemplateSlug": "comments",
	"PaginationLimit": 5,
	"PaginationColumns": ["_id"],
	"Paginate": true,
	"Fields": [
		{
			"ParameterName": "postId",
			"DocumentFieldName": "postId",
			"MatchKind": 0,
			"BsonType": 7,
			"IsNullable": false,
			"IsOptional": false,
			"Value": null
		}
	]
}
EOF
)
curl -k --request POST \
  --url $api_url/api/v1/routes \
  --header "Authorization: Bearer $jwt" \
  --header "Content-Type: application/json" \
  --data "$json_body"
  
json_body=$(cat <<EOF
{
	"UrlTemplate": "login",
	"VirtualPathTemplate": "static-content/login.html",
	"RedirectUrlTemplate": null,
	"CollectionSlug": null,
	"TemplateSlug": null,
	"BaseTemplatePathTemplate": "static-content/blog.html",
	"Paginate": false,
	"Fields": []
}
EOF
)  
curl -k --request POST \
  --url $api_url/api/v1/routes \
  --header "Authorization: Bearer $jwt" \
  --header "Content-Type: application/json" \
  --data "$json_body"

json_body=$(cat <<EOF
{
	"UrlTemplate": "register",
	"VirtualPathTemplate": "static-content/register.html",
	"RedirectUrlTemplate": null,
	"CollectionSlug": null,
	"TemplateSlug": null,
	"BaseTemplatePathTemplate": "static-content/blog.html",
	"Paginate": false,
	"Fields": []
}
EOF
)  
curl -k --request POST \
  --url $api_url/api/v1/routes \
  --header "Authorization: Bearer $jwt" \
  --header "Content-Type: application/json" \
  --data "$json_body"
  
json_body=$(cat <<EOF
{  
	"UrlTemplate": "/",
	"VirtualPathTemplate": null,
	"RedirectUrlTemplate": "/blog/posts",
	"CollectionSlug": null,
	"TemplateSlug": null,
	"BaseTemplatePathTemplate": null,
	"Paginate": false,
	"Fields": []
}
EOF
)  
curl -k --request POST \
  --url $api_url/api/v1/routes \
  --header "Authorization: Bearer $jwt" \
  --header "Content-Type: application/json" \
  --data "$json_body"

json_body=$(cat <<EOF
{
	"UrlTemplate": "blog/create-post",
	"VirtualPathTemplate": "static-content/create-post.html",
	"RedirectUrlTemplate": null,
	"CollectionSlug": null,
	"TemplateSlug": null,
	"BaseTemplatePathTemplate": "static-content/blog.html",
	"Paginate": false,
	"PaginationLimit": 1,
	"PaginateAscending": false,
	"Fields": []
}
EOF
)  
curl -k --request POST \
  --url $api_url/api/v1/routes \
  --header "Authorization: Bearer $jwt" \
  --header "Content-Type: application/json" \
  --data "$json_body"  
  
curl -k --request POST \
  --url "$api_url/api/v1/collections/users/templates" \
  --header 'Content-Type: multipart/form-data' \
  --header "Authorization: Bearer $jwt" \
  --form "Slug=post-author-template" \
  --form "SingleItem=true" \
  --form "templateFile=@./post_author_template.html"
  
curl -k --request POST \
  --url "$api_url/api/v1/collections/users/templates" \
  --header 'Content-Type: multipart/form-data' \
  --header "Authorization: Bearer $jwt" \
  --form "Slug=comment-author-template" \
  --form "SingleItem=true" \
  --form "templateFile=@./comment_author_template.html"

json_body=$(cat <<EOF
{
	"UrlTemplate": "users/{userId}/{templateSlug}",
	"CollectionSlug": "users",
	"Paginate": false,
	"Fields": [
		{
			"ParameterName": "userId",
			"DocumentFieldName": "_id",
			"MatchKind": 0,
			"BsonType": 7,
			"IsNullable": false,
			"IsOptional": false,
			"Value": null
		}
	]
}
EOF
)
curl -k --request POST \
  --url $api_url/api/v1/routes \
  --header "Authorization: Bearer $jwt" \
  --header "Content-Type: application/json" \
  --data "$json_body"

curl -k --request POST \
  --url $api_url/api/v1/collections/posts \
  --header "Authorization: Bearer $jwt" \
  --form "title=My first post" \
  --form "slug=my-first-post" \
  --form "summary=My first post on this blog" \
  --form "content=# My First Post <br> This is my first post on my new fancy blog, please lets be polite and enjoy ourselves ^^"